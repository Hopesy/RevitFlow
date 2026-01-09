using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Microsoft.Extensions.Logging;
using RevitFlow.Commands;
using Tuna.Revit.Extensions;
using RevitFlow.Services;

namespace RevitFlow;

public class Application : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        Host.Start();
        var logger = Host.GetService<ILogger<Application>>();

        // 初始化程序集解析器
        AssemblyResolver.Initialize(logger);
        
        // 创建UI面板，添加按钮
        this.CreateRibbon(application);
        
        logger.LogInformation("RevitFlow 插件启动");
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        var logger = Host.GetService<ILogger<Application>>();
        logger.LogInformation("RevitFlow 插件关闭");

        AssemblyResolver.Cleanup();
        Host.Stop();
        return Result.Succeeded;
    }

    private void CreateRibbon(UIControlledApplication application)
    {
        var tab = application.AddRibbonTab("RevitFlow");

        // 建模工具面板
        tab.AddRibbonPanel("建模工具", panel =>
        {
            panel.AddPushButton<WallOpeningCommand>(button =>
            {
                button.LargeImage = new BitmapImage(
                    new Uri("pack://application:,,,/RevitFlow;component/resources/icons/window.png"));
                button.ToolTip = "在墙体上快速创建洞口";
                button.Title = "墙体开洞";
            });

            panel.AddPushButton<CurveArrayCommand>(button =>
            {
                button.LargeImage = new BitmapImage(
                    new Uri("pack://application:,,,/RevitFlow;component/resources/icons/array.png"));
                button.ToolTip = "沿曲线阵列族实例";
                button.Title = "曲线阵列";
            });
        });

        // 设置面板
        tab.AddRibbonPanel("设置", panel =>
        {
            panel.AddPushButton<SettingCommand>(button =>
            {
                button.LargeImage = new BitmapImage(
                    new Uri("pack://application:,,,/RevitFlow;component/resources/icons/setting.png"));
                button.ToolTip = "插件设置";
                button.Title = "设置";
            });
        });
    }
}