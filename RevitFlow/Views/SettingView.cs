using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;

namespace RevitFlow.Views;

/// <summary>
/// 设置窗口
/// </summary>
public class SettingView : WebViewWindowBase
{
    public SettingView(
        ILogger<WebViewWindowBase> logger,
        SettingViewModel viewModel)
        : base(logger, viewModel)
    {
        SetPageName("index.html?page=setting");
    }
}
