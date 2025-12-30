using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace RevitFlow.Views;

public partial class WebViewWindowBase : Window
{
    private readonly ILogger<WebViewWindowBase> _logger;
    private readonly ObservableObject _viewModel;
    private string _pageName = "index.html";

    public WebViewWindowBase(ILogger<WebViewWindowBase> logger, ObservableObject viewModel)
    {
        _logger = logger;
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void SetPageName(string pageName)
    {
        _pageName = pageName;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        // 清理资源
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
#if DEBUG
        settings.AreDevToolsEnabled = true;
#else
        settings.AreDevToolsEnabled = false;
#endif

        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var webRoot = GetWebRootPath();
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "revitflow.local", webRoot, CoreWebView2HostResourceAccessKind.Allow);

        await InjectBridgeScriptAsync();

        var pagePath = Path.Combine(webRoot, _pageName);
        if (File.Exists(pagePath))
        {
            WebView.CoreWebView2.Navigate($"https://revitflow.local/{_pageName}");
        }
        else
        {
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
                    window.chrome.webview.postMessage({
                        messageType: messageType,
                        payload: payload || {}
                    });
                }
            };
            console.log('RevitBridge 已注入');
            """;

        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            _logger.LogDebug("收到消息: {Message}", message);

            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            var messageType = root.GetProperty("messageType").GetString();
            var payload = root.GetProperty("payload");

            HandleMessage(messageType!, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 WebMessage 失败");
        }
    }

    private void HandleMessage(string messageType, JsonElement payload)
    {
        _logger.LogDebug("处理消息: {Type}", messageType);

        switch (messageType)
        {
            case "setState":
                SetViewModelState(payload);
                break;

            case "invokeCommand":
                var commandName = payload.GetProperty("command").GetString();
                var commandParam = payload.TryGetProperty("param", out var p) ? p.GetString() : null;
                _logger.LogDebug("执行命令: {Command}", commandName);
                InvokeCommand(commandName!, commandParam);
                break;
        }
    }

    private void SetViewModelState(JsonElement payload)
    {
        var vmType = _viewModel.GetType();

        foreach (var prop in payload.EnumerateObject())
        {
            var propName = char.ToUpperInvariant(prop.Name[0]) + prop.Name[1..];
            var vmProp = vmType.GetProperty(propName);

            if (vmProp != null && vmProp.CanWrite)
            {
                var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), vmProp.PropertyType);
                vmProp.SetValue(_viewModel, value);
                _logger.LogDebug("设置属性: {PropName} = {Value}", propName, value);
            }
            else
            {
                _logger.LogWarning("无法设置属性: {PropName}", propName);
            }
        }
    }

    private void InvokeCommand(string commandName, string? param)
    {
        var vmType = _viewModel.GetType();
        var commandProp = vmType.GetProperty(commandName + "Command");

        if (commandProp?.GetValue(_viewModel) is System.Windows.Input.ICommand command)
        {
            if (command.CanExecute(param))
            {
                command.Execute(param);
            }
        }
    }

    private static string GetWebRootPath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(assemblyDir, "Web");
    }
}
