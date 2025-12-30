using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;

namespace RevitFlow.Views;

/// <summary>
/// 设置窗口
/// </summary>
public class SettingWindow : WebViewWindowBase
{
    public SettingWindow(
        ILogger<WebViewWindowBase> logger,
        SettingViewModel viewModel)
        : base(logger, viewModel)
    {
        SetPageName("setting.html");
    }
}
