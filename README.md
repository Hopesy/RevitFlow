# RevitFlow

基于 WebView2 + Vue 3 的 Revit 插件开发框架，采用 MVVM 架构实现 C# 与 Vue 的双向数据绑定。

## 项目结构

```
RevitFlow/                    # C# Revit 插件项目
├── Commands/                 # Revit 命令 (按钮入口)
│   ├── WallOpeningCommand.cs
│   └── SettingCommand.cs
├── ViewModels/               # MVVM ViewModel 层
│   ├── WallOpeningViewModel.cs
│   └── SettingViewModel.cs
├── Views/                    # WebView2 窗口
│   └── WebViewWindow.xaml
├── Web/                      # Vue 构建产物 (自动生成)
└── Application.cs            # 插件入口

RevitVue/                     # Vue 3 前端项目
├── src/
│   ├── composables/
│   │   └── useRevitBridge.js # C# 通信桥接
│   └── App.vue               # 页面组件
└── vite.config.js
```

## 架构原理

### MVVM 架构

```
┌─────────────────────────────────────────────────────────┐
│                      Revit 插件                          │
│  ┌──────────┐    ┌─────────────┐    ┌────────────────┐  │
│  │ Command  │───▶│  ViewModel  │◀──▶│ WebViewWindow  │  │
│  │ (按钮)   │    │  (业务逻辑)  │    │   (WebView2)   │  │
│  └──────────┘    └─────────────┘    └───────┬────────┘  │
│                         ▲                    │           │
│                         │ 双向绑定            │ 加载      │
│                         ▼                    ▼           │
│                  ┌─────────────────────────────┐        │
│                  │      Vue 页面 (Web/)        │        │
│                  │   useRevitBridge.js         │        │
│                  └─────────────────────────────┘        │
└─────────────────────────────────────────────────────────┘
```

### 通信机制

Vue 与 C# ViewModel 通过 WebView2 的 `postMessage` 实现双向通信：

#### 1. 获取状态 (getState)

```javascript
// Vue 端
const { state } = useRevitBridge()
// state.width, state.height 等属性自动从 ViewModel 同步
```

```csharp
// C# ViewModel
[ObservableProperty] private double _width = 1000;
[ObservableProperty] private double _height = 2100;
```

#### 2. 更新状态 (setState)

```javascript
// Vue 端 - 更新 ViewModel 属性
await setState({ width: 1500, height: 2000 })
```

#### 3. 调用命令 (invokeCommand)

```javascript
// Vue 端 - 调用 ViewModel 的 RelayCommand
await invokeCommand('CreateOpening')
await invokeCommand('ApplyPreset', 'door')
```

```csharp
// C# ViewModel
[RelayCommand]
private async Task CreateOpeningAsync() { ... }

[RelayCommand]
private void ApplyPreset(string preset) { ... }
```

### 消息格式

```
Vue → C#:
{
  "callbackId": "1234_0.567",
  "messageType": "invokeCommand",
  "payload": { "command": "CreateOpening" }
}

C# → Vue:
{
  "callbackId": "1234_0.567",
  "success": true,
  "data": { "width": 1000, "height": 2100, ... }
}
```

## 开发流程

### 1. 添加新功能

```csharp
// 1. 创建 ViewModel
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] private string _name;
    
    [RelayCommand]
    private void DoSomething() { ... }
}

// 2. 创建 Command
[Transaction(TransactionMode.Manual)]
public class MyCommand : IExternalCommand
{
    public Result Execute(...)
    {
        var vm = Host.GetService<MyViewModel>();
        var window = Host.GetService<WebViewWindow>();
        window.SetViewModel(vm, "my-page.html");
        window.Show();
        return Result.Succeeded;
    }
}

// 3. 在 Host.cs 注册
builder.Services.AddTransient<MyViewModel>();

// 4. 在 Application.cs 添加按钮
panel.AddPushButton<MyCommand>(...);
```

### 2. Vue 开发

```bash
cd RevitVue
npm run dev      # 开发模式 (带热更新)
npm run build    # 构建到 RevitFlow/Web/
```

### 3. 编译运行

1. `npm run build` 构建 Vue
2. Visual Studio 编译 C# 项目
3. 启动 Revit 测试

## 技术栈

**C# 端:**
- .NET 8.0 + WPF
- Revit API 2026
- CommunityToolkit.Mvvm (MVVM 框架)
- Microsoft.Web.WebView2
- Microsoft.Extensions.Hosting (依赖注入)
- Serilog (日志)

**Vue 端:**
- Vue 3.5 + Composition API
- Vite 7

## Revit 线程处理

Revit API 必须在主线程调用，使用 `ExternalEvent` 机制：

```csharp
// ViewModel 中
private RevitExternalEventHandler? _handler;
private ExternalEvent? _externalEvent;

public void Initialize(UIDocument uiDoc)
{
    _handler = new RevitExternalEventHandler();
    _externalEvent = ExternalEvent.Create(_handler);
}

[RelayCommand]
private async Task CreateOpeningAsync()
{
    await _handler.ExecuteAsync(_externalEvent, () =>
    {
        // 这里的代码在 Revit 主线程执行
        var reference = _uiDocument.Selection.PickObject(...);
    });
}
```
