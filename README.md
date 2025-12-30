# RevitFlow

基于 WebView2 + Vue 3 的 Revit 插件开发框架，采用 MVVM 架构实现 C# 与 Vue 的双向数据绑定。

## 特性

- ✅ **单页应用架构** - 所有窗口共用一个 `index.html`，通过 URL 参数路由
- ✅ **MVVM 双向绑定** - Vue 与 C# ViewModel 自动同步状态
- ✅ **依赖注入** - 基于 Microsoft.Extensions.Hosting
- ✅ **热更新开发** - Vue 开发模式支持热更新
- ✅ **类型安全** - C# 12 + Vue 3 Composition API

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
│   ├── WebViewWindowBase.xaml      # 窗口基类
│   ├── WebViewWindowBase.xaml.cs
│   ├── WallOpeningWindow.cs        # 墙体开洞窗口
│   └── SettingWindow.cs            # 设置窗口
├── Services/                 # 业务服务
│   └── WallOpeningExternalEvent.cs # Revit 外部事件处理
├── Web/                      # Vue 构建产物 (自动生成)
│   ├── index.html
│   ├── js/
│   └── css/
├── Host.cs                   # 依赖注入容器
└── Application.cs            # 插件入口

revitvue/                     # Vue 3 前端项目
├── src/
│   ├── components/
│   │   ├── WallOpeningPage.vue    # 墙体开洞页面
│   │   └── SettingPage.vue        # 设置页面
│   ├── composables/
│   │   └── useRevitBridge.js      # C# 通信桥接
│   ├── App.vue                    # 路由容器
│   └── main.js
└── vite.config.js
```

## 架构原理

### 单页应用 (SPA) 架构

所有窗口共用一个 `index.html`，通过 URL 参数区分页面：

```
WallOpeningWindow  →  index.html?page=wall-opening  →  WallOpeningPage.vue
SettingWindow      →  index.html?page=setting       →  SettingPage.vue
```

**优势：**
- 只需构建一次，生成一个 HTML 文件
- 代码复用，共享样式和逻辑
- 易于扩展新页面

### MVVM 架构

```
┌─────────────────────────────────────────────────────────────┐
│                      Revit 插件                              │
│  ┌──────────┐    ┌─────────────┐    ┌──────────────────┐    │
│  │ Command  │───▶│  ViewModel  │◀──▶│ WebViewWindow    │    │
│  │ (按钮)   │    │  (状态管理)  │    │   (WebView2)     │    │
│  └──────────┘    └──────┬──────┘    └────────┬─────────┘    │
│                         │                     │              │
│                         │ 更新参数             │ 加载         │
│                         ▼                     ▼              │
│                  ┌─────────────┐    ┌──────────────────┐    │
│                  │   Handler   │    │  Vue 页面 (Web/) │    │
│                  │ (Revit API) │    │  useRevitBridge  │    │
│                  └─────────────┘    └──────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

**关键设计：**
- `ViewModel` 管理 UI 状态（宽度、高度等）
- `ExternalEvent Handler` 独立处理 Revit API 调用
- `ViewModel` 在调用前更新 `Handler` 的参数
- 避免循环依赖

### 通信机制

Vue 与 C# ViewModel 通过 WebView2 的 `postMessage` 实现双向通信：

#### 1. 状态同步 (setState)

```javascript
// Vue 端 - 更新 ViewModel 属性
const { state, setState } = useRevitBridge({ width: 1000, height: 2100 })
await setState({ width: 1500 })
```

```csharp
// C# ViewModel - 属性自动更新
[ObservableProperty] private double _width = 1000;
[ObservableProperty] private double _height = 2100;
```

#### 2. 调用命令 (invokeCommand)

```javascript
// Vue 端 - 调用 ViewModel 的 RelayCommand
await invokeCommand('CreateOpening')
await invokeCommand('ApplyPreset', 'door')
```

```csharp
// C# ViewModel
[RelayCommand]
private void CreateOpening()
{
    // 更新 Handler 参数
    _handler.Width = Width;
    _handler.Height = Height;

    // 触发外部事件
    _externalEvent.Raise();
}
```

### 消息格式

```javascript
// Vue → C#
{
  "messageType": "setState",
  "payload": { "width": 1500, "height": 2000 }
}

{
  "messageType": "invokeCommand",
  "payload": { "command": "CreateOpening", "param": "door" }
}
```

## 开发流程

### 1. 添加新功能

#### 步骤 1: 创建 ViewModel

```csharp
public partial class MyViewModel : ObservableObject
{
    private readonly ILogger<MyViewModel> _logger;
    private readonly ExternalEvent _externalEvent;
    private readonly MyExternalEvent _handler;

    [ObservableProperty] private string _name = "";

    public MyViewModel(
        ILogger<MyViewModel> logger,
        ExternalEvent externalEvent,
        MyExternalEvent handler)
    {
        _logger = logger;
        _externalEvent = externalEvent;
        _handler = handler;
    }

    [RelayCommand]
    private void DoSomething()
    {
        // 更新 handler 参数
        _handler.Name = Name;

        // 触发外部事件
        _externalEvent.Raise();
    }
}
```

#### 步骤 2: 创建 ExternalEvent Handler

```csharp
public class MyExternalEvent : IExternalEventHandler
{
    private readonly ILogger<MyExternalEvent> _logger;

    public string Name { get; set; } = "";

    public MyExternalEvent(ILogger<MyExternalEvent> logger)
    {
        _logger = logger;
    }

    public void Execute(UIApplication app)
    {
        // 在 Revit 主线程执行
        var uiDoc = app.ActiveUIDocument;
        // ... Revit API 调用
    }

    public string GetName() => "My External Event";
}
```

#### 步骤 3: 创建窗口

```csharp
public class MyWindow : WebViewWindowBase
{
    public MyWindow(
        ILogger<WebViewWindowBase> logger,
        MyViewModel viewModel)
        : base(logger, viewModel)
    {
        SetPageName("index.html?page=my-page");
    }
}
```

#### 步骤 4: 创建 Command

```csharp
[Transaction(TransactionMode.Manual)]
public class MyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var window = Host.GetService<MyWindow>();
            window.Title = "我的功能";
            window.Show();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
```

#### 步骤 5: 注册服务

```csharp
// Host.cs
builder.Services.AddTransient<MyViewModel>();
builder.Services.AddTransient<MyWindow>();
builder.Services.AddSingleton<MyExternalEvent>();

builder.Services.AddSingleton(sp =>
{
    var handler = sp.GetRequiredService<MyExternalEvent>();
    return ExternalEvent.Create(handler);
});
```

#### 步骤 6: 添加按钮

```csharp
// Application.cs
panel.AddPushButton<MyCommand>("我的功能", "MyButton", iconPath);
```

#### 步骤 7: 创建 Vue 页面

```vue
<!-- src/components/MyPage.vue -->
<script setup>
import { useRevitBridge } from '../composables/useRevitBridge'

const { state, setState, invokeCommand } = useRevitBridge({ name: '' })

function doSomething() {
  invokeCommand('DoSomething')
}
</script>

<template>
  <div>
    <input v-model="state.name" @change="setState({ name: state.name })" />
    <button @click="doSomething">执行</button>
  </div>
</template>
```

#### 步骤 8: 注册路由

```vue
<!-- src/App.vue -->
<script setup>
import MyPage from './components/MyPage.vue'
// ...
</script>

<template>
  <div class="app">
    <MyPage v-if="currentPage === 'my-page'" />
    <!-- ... -->
  </div>
</template>
```

### 2. Vue 开发

```bash
cd revitvue
npm install      # 安装依赖
npm run dev      # 开发模式 (带热更新，访问 http://localhost:5173)
npm run build    # 构建到 RevitFlow/Web/
```

### 3. 编译运行

```bash
# 方式 1: 使用 dotnet CLI
dotnet build     # 自动构建 Vue + 编译 C#

# 方式 2: 使用 Visual Studio
# 打开 RevitFlow.sln，按 F5 编译
```

启动 Revit 测试插件。

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
// ViewModel 触发事件
[RelayCommand]
private void CreateOpening()
{
    _handler.Width = Width;
    _handler.Height = Height;
    _externalEvent.Raise();  // 异步触发
}

// Handler 在 Revit 主线程执行
public class WallOpeningExternalEvent : IExternalEventHandler
{
    public double Width { get; set; }
    public double Height { get; set; }

    public void Execute(UIApplication app)
    {
        // 这里的代码在 Revit 主线程执行
        var uiDoc = app.ActiveUIDocument;
        var reference = uiDoc.Selection.PickObject(...);

        using var trans = new Transaction(uiDoc.Document, "创建洞口");
        trans.Start();
        // ... Revit API 调用
        trans.Commit();
    }

    public string GetName() => "Wall Opening Event";
}
```

## 调试技巧

### 1. 启用 WebView2 开发者工具

```csharp
// WebViewWindowBase.xaml.cs
#if DEBUG
settings.AreDevToolsEnabled = true;  // 右键 → 检查
#endif
```

### 2. 查看日志

日志文件位置：`C:\ProgramData\Autodesk\Revit\Addins\2026\RevitFlow\Logs\`

### 3. Vue 开发模式

```bash
cd revitvue
npm run dev
# 访问 http://localhost:5173 进行调试
```

## 常见问题

### Q: 窗口一直显示"加载中"？
A: 检查：
1. Vue 是否已构建 (`npm run build`)
2. `Web/index.html` 是否存在
3. 查看日志文件排查错误

### Q: 如何添加新页面？
A:
1. 创建 Vue 组件 (`src/components/MyPage.vue`)
2. 在 `App.vue` 中注册路由
3. 创建窗口类，调用 `SetPageName("index.html?page=my-page")`

### Q: 如何调用 Revit API？
A: 必须通过 `ExternalEvent` 在主线程调用，参考"Revit 线程处理"章节。

## 许可证

MIT License
