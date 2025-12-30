using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;
using RevitFlow.Views;
using Serilog;
using System.IO;
using System.Reflection;

namespace RevitFlow;

public static class Host
{
    private static IHost? host;
    public static void Start()
    {
        //【1】使用默认配置创建主机
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            // 插件环境下默认的ContentRootPathRevit指向Revit.exe，因此这里需要修改为插件dll目录
            ContentRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        });

        //【2】配置日志为Serilog（写入文件和控制台）
        builder.Logging.ClearProviders();
        var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "Logs");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, "RevitFlow.log");
        builder.Logging.AddSerilog(new LoggerConfiguration()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger());

        //【3】注册服务
        // ViewModels
        builder.Services.AddTransient<WallOpeningViewModel>();
        builder.Services.AddTransient<SettingViewModel>();

        // Views
        builder.Services.AddTransient<WebViewWindow>();

        // Services
        builder.Services.AddSingleton<Services.WallOpeningExternalEvent>();

        host = builder.Build();
        host.Start();
    }
    public static void Stop()
    {
        //GetAwaiter()：获取 Task 的等待器;GetResult()：阻塞当前线程，直到StopAsync()完成
        host!.StopAsync().GetAwaiter().GetResult();
    }
    public static T GetService<T>() where T : class => host!.Services.GetRequiredService<T>();
    public static IServiceProvider Services
    {
        get => host?.Services ?? throw new InvalidOperationException("Host is null.");
    }
}