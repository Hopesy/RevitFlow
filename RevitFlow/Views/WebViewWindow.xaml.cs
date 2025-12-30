using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using RevitFlow.ViewModels;

namespace RevitFlow.Views;

public partial class WebViewWindow : Window
{
    private readonly ILogger<WebViewWindow> _logger;
    private ObservableObject? _viewModel;
    private string _pageName = "index.html";
    private Action? _closeHandler;

    public WebViewWindow(ILogger<WebViewWindow> logger)
    {
        InitializeComponent();
        _logger = logger;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    /// <summary>
    /// 设置 ViewModel
    /// </summary>
    public void SetViewModel(ObservableObject viewModel, string pageName = "index.html")
    {
        _viewModel = viewModel;
        _pageName = pageName;
        DataContext = viewModel;

        // 订阅关闭请求事件
        if (viewModel is WallOpeningViewModel wallVm)
        {
            _closeHandler = () => Dispatcher.Invoke(Close);
            wallVm.RequestClose += _closeHandler;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        // 取消订阅事件
        if (_viewModel is WallOpeningViewModel wallVm && _closeHandler != null)
        {
            wallVm.RequestClose -= _closeHandler;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await InitializeWebViewAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebView2 初始化失败");
            LoadingText.Text = $"加载失败: {ex.Message}";
        }
    }

    private async Task InitializeWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RevitFlow", "WebView2");

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await WebView.EnsureCoreWebView2Async(env);

        var settings = WebView.CoreWebView2.Settings;
        settings.IsScriptEnabled = true;
        settings.IsWebMessageEnabled = true;
        settings.AreDefaultContextMenusEnabled = false;
        settings.IsStatusBarEnabled = false;
#if DEBUG
        settings.AreDevToolsEnabled = true;
#else
        settings.AreDevToolsEnabled = false;
#endif

        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        
        // 设置虚拟主机映射，解决本地文件加载问题
        var webRoot = GetWebRootPath();
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "revitflow.local", 
            webRoot, 
            CoreWebView2HostResourceAccessKind.Allow);

        await InjectBridgeScriptAsync();

        var pagePath = Path.Combine(webRoot, _pageName);

        if (File.Exists(pagePath))
        {
            WebView.CoreWebView2.Navigate($"https://revitflow.local/{_pageName}");
            _logger.LogInformation("加载页面: {Path}", pagePath);
        }
        else
        {
            _logger.LogWarning("页面不存在: {Path}", pagePath);
            LoadingText.Text = $"页面不存在: {_pageName}";
            return;
        }

        LoadingText.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Visible;
    }

    private async Task InjectBridgeScriptAsync()
    {
        const string bridgeScript = """
            window.RevitBridge = {
                invoke: function(messageType, payload) {
                    return new Promise((resolve, reject) => {
                        const callbackId = Date.now() + '_' + Math.random();
                        window._revitCallbacks = window._revitCallbacks || {};
                        window._revitCallbacks[callbackId] = { resolve, reject };
                        
                        window.chrome.webview.postMessage(JSON.stringify({
                            callbackId: callbackId,
                            messageType: messageType,
                            payload: payload || {}
                        }));
                    });
                }
            };
            
            window.chrome.webview.addEventListener('message', function(e) {
                const data = JSON.parse(e.data);
                if (data.callbackId && window._revitCallbacks[data.callbackId]) {
                    const callback = window._revitCallbacks[data.callbackId];
                    delete window._revitCallbacks[data.callbackId];
                    
                    if (data.success) {
                        callback.resolve(data.data);
                    } else {
                        callback.reject(new Error(data.error));
                    }
                } else if (data.event) {
                    window.dispatchEvent(new CustomEvent('revit:' + data.event, { detail: data.data }));
                }
            });
            
            console.log('RevitBridge 已注入');
            """;

        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            var callbackId = root.GetProperty("callbackId").GetString();
            var messageType = root.GetProperty("messageType").GetString();
            var payload = root.GetProperty("payload");

            var result = await HandleViewModelMessageAsync(messageType!, payload);

            var response = $$"""{"callbackId":"{{callbackId}}",{{result.TrimStart('{').TrimEnd('}')}}}""";
            WebView.CoreWebView2.PostWebMessageAsJson(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 WebMessage 失败");
        }
    }

    private async Task<string> HandleViewModelMessageAsync(string messageType, JsonElement payload)
    {
        if (_viewModel == null)
        {
            return JsonSerializer.Serialize(new { success = false, error = "ViewModel 未设置" });
        }

        try
        {
            switch (messageType)
            {
                case "getState":
                    return GetViewModelState();

                case "setState":
                    SetViewModelState(payload);
                    return JsonSerializer.Serialize(new { success = true, data = new { } });

                case "invokeCommand":
                    var commandName = payload.GetProperty("command").GetString();
                    var commandParam = payload.TryGetProperty("param", out var p) ? p.GetString() : null;
                    await InvokeCommandAsync(commandName!, commandParam);
                    return GetViewModelState();

                default:
                    return JsonSerializer.Serialize(new { success = false, error = $"未知消息类型: {messageType}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 ViewModel 消息失败");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    private string GetViewModelState()
    {
        var vmType = _viewModel!.GetType();
        var state = new Dictionary<string, object?>();

        foreach (var prop in vmType.GetProperties())
        {
            if (prop.CanRead && prop.Name != "HasErrors")
            {
                try
                {
                    var value = prop.GetValue(_viewModel);
                    var name = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
                    state[name] = value;
                }
                catch { }
            }
        }

        return JsonSerializer.Serialize(new { success = true, data = state });
    }

    private void SetViewModelState(JsonElement payload)
    {
        var vmType = _viewModel!.GetType();

        foreach (var prop in payload.EnumerateObject())
        {
            var propName = char.ToUpperInvariant(prop.Name[0]) + prop.Name[1..];
            var vmProp = vmType.GetProperty(propName);

            if (vmProp != null && vmProp.CanWrite)
            {
                var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), vmProp.PropertyType);
                vmProp.SetValue(_viewModel, value);
            }
        }
    }

    private async Task InvokeCommandAsync(string commandName, string? param)
    {
        var vmType = _viewModel!.GetType();
        
        var commandProp = vmType.GetProperty(commandName + "Command");
        if (commandProp?.GetValue(_viewModel) is System.Windows.Input.ICommand command)
        {
            if (command.CanExecute(param))
            {
                if (command is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand asyncCommand)
                {
                    await asyncCommand.ExecuteAsync(param);
                }
                else
                {
                    command.Execute(param);
                }
            }
        }
    }

    private static string GetWebRootPath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(assemblyDir, "Web");
    }
}
