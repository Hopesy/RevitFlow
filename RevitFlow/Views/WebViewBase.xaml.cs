using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace RevitFlow.Views;

public partial class WebViewBase : Window
{
    private readonly ILogger<WebViewBase> _logger;
    private readonly ObservableObject _viewModel;
    private string _pageName = "index.html";

    public WebViewBase(ILogger<WebViewBase> logger, ObservableObject viewModel)
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

    /// <summary>
    /// C# 主动向 JavaScript 发送数据
    /// </summary>
    public async Task SendToJavaScriptAsync(string functionName, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            // 转义 JSON 中的特殊字符
            json = json.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");

            var script = $"if (window.{functionName}) {{ window.{functionName}('{json}'); }}";
            await WebView.CoreWebView2.ExecuteScriptAsync(script);

            _logger.LogDebug("发送数据到 JS: {FunctionName}", functionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送数据到 JS 失败");
        }
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
        //用户数据目录：存储WebView2的缓存、Cookie、LocalStorage等
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RevitFlow", "WebView2");
        //创建环境，使用系统默认的Edge WebView2 Runtime;指定数据目录
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        //初始化WebView2控件的核心功能
        await WebView.EnsureCoreWebView2Async(env);
        var settings = WebView.CoreWebView2.Settings;
        settings.IsScriptEnabled = true;                   // 启用 JavaScript
        settings.IsWebMessageEnabled = true;               // 启用消息通信
        settings.AreDefaultContextMenusEnabled = false;    // 禁用右键菜单
        settings.AreDevToolsEnabled = false;               // 禁用F12开发者工具
        //【重点】注册消息接收事件
        //当Js调用window.chrome.webview.postMessage()时触发,在C#端接收并处理消息
        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var webRoot = Path.Combine(assemblyDir, "Web");
        //【重点】虚拟主机名:revitflow.local-自定义的域名不需要DNS解析,仅在WebView2内部有效
        //CoreWebView2HostResourceAccessKind.Allow允许访问映射目录下的所有文件
        //映射目录:https://revitflow.local/index.html → C:\...\RevitFlow\Web\index.html
        //file://协议有安全限制,无法使用现代Web特性,因此采用了虚拟主机模式
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "revitflow.local", webRoot, CoreWebView2HostResourceAccessKind.Allow);
        //注入桥接代码
        await InjectBridgeScriptAsync();
        // 分离文件名和 URL 参数
        var fileName = _pageName.Split('?')[0];
        var pagePath = Path.Combine(webRoot, fileName);
        if (File.Exists(pagePath))
        {
            WebView.CoreWebView2.Navigate($"https://revitflow.local/{_pageName}");
        }
        else
        {
            LoadingText.Text = $"页面不存在: {fileName}";
            return;
        }
        LoadingText.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Visible;
    }
    //【重点】JavaScript桥接机制:注入桥接脚本
    /*
     * // 消息格式
     *  {
     *   "messageType": "setState",
     *   "payload": { "width": 1500, "height": 2000 }
     *  }
     *  { "messageType": "invokeCommand",
     *   "payload": { "command": "CreateOpening", "param": "door" }
     *  }
     * // Vue端调用
     * window.RevitBridge.invoke('setState', { width: 1500 });
     * window.RevitBridge.invoke('invokeCommand', { command: 'CreateOpening' });
     */
    //注入桥接脚本后,Vue不需要直接调用WebView2 API,更简洁
    private async Task InjectBridgeScriptAsync()
    {
        //定义桥接对象RevitBridge
        //提供统一的调用接口,简化Vue端的调用代码
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
        //在每个页面加载前自动执行脚本,即使页面刷新或导航,脚本也会重新注入,确保window.RevitBridge始终可用
        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);
    }
    // C#与JavaScript双向通信
    // 接收JavaScript发送的JSON消息并解析
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
        _logger.LogInformation("处理消息: {Type}", messageType);
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
            case "log":
                // 处理来自 JavaScript 的日志
                var level = payload.TryGetProperty("level", out var lvl) ? lvl.GetString() : "info";
                var logMessage = payload.GetProperty("message").GetString();
                LogFromJavaScript(level!, logMessage!);
                break;
        }
    }

    /// <summary>
    /// 处理来自 JavaScript 的日志消息
    /// </summary>
    private void LogFromJavaScript(string level, string message)
    {
        switch (level.ToLower())
        {
            case "debug":
                _logger.LogDebug("[JS] {Message}", message);
                break;
            case "info":
                _logger.LogInformation("[JS] {Message}", message);
                break;
            case "warn":
                _logger.LogWarning("[JS] {Message}", message);
                break;
            case "error":
                _logger.LogError("[JS] {Message}", message);
                break;
            default:
                _logger.LogInformation("[JS] {Message}", message);
                break;
        }
    }

    private void SetViewModelState(JsonElement payload)
    {
        var vmType = _viewModel.GetType();
        foreach (var prop in payload.EnumerateObject())
        {   
            // 1.将camelCase转换为PascalCase
            var propName = char.ToUpperInvariant(prop.Name[0]) + prop.Name[1..];
            // 2.通过反射获取属性,根据名称查找属性
            var vmProp = vmType.GetProperty(propName);
            if (vmProp != null && vmProp.CanWrite)
            {  
                // 3.反序列化JSON值为目标类型
                //vmProp.PropertyType:获取属性的实际类型如double,string
                var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), vmProp.PropertyType);
                //C#反射不仅可以获取值，还可以设置实例的属性和字段值
                vmProp.SetValue(_viewModel, value);
                // 4.通过反射设置属性值
                _logger.LogInformation("设置属性: {PropName} = {Value}", propName, value);
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
        // 1.查找命令属性(CommunityToolkit生成的命令名称为"XxxCommand")
        var commandProp = vmType.GetProperty(commandName + "Command");
        // 2. 获取命令实例并执行
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
