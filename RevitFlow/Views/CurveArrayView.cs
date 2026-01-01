using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;

namespace RevitFlow.Views;

/// <summary>
/// 曲线阵列窗口
/// </summary>
public class CurveArrayView : WebViewBase
{
    public CurveArrayView(ILogger<WebViewBase> logger, CurveArrayViewModel viewModel) : base(logger, viewModel)
    {
        SetPageName("index.html?page=curve-array");
    }
}
