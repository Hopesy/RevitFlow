using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;

namespace RevitFlow.Views;

/// <summary>
/// 墙体开洞窗口
/// </summary>
public class WallOpeningView : WebViewWindowBase
{
    public WallOpeningView(
        ILogger<WebViewWindowBase> logger,
        WallOpeningViewModel viewModel)
        : base(logger, viewModel)
    {
        SetPageName("index.html?page=wall-opening");
    }
}
