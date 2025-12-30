using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitFlow.Services;
using RevitFlow.ViewModels;
using RevitFlow.Views;

namespace RevitFlow.Commands;

[Transaction(TransactionMode.Manual)]
public class WallOpeningCommand : IExternalCommand
{
    private static ExternalEvent? _externalEvent;
    private static WallOpeningExternalEvent? _eventHandler;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiDoc = commandData.Application.ActiveUIDocument;

            // 初始化外部事件（只创建一次）
            if (_externalEvent == null || _eventHandler == null)
            {
                _eventHandler = Host.GetService<WallOpeningExternalEvent>();
                _externalEvent = ExternalEvent.Create(_eventHandler);
            }

            // 获取 ViewModel
            var viewModel = Host.GetService<WallOpeningViewModel>();
            viewModel.SetExternalEvent(_externalEvent);

            // 设置外部事件参数
            _eventHandler.SetParameters(viewModel, uiDoc);

            // 创建非模态窗口
            var window = Host.GetService<WebViewWindow>();
            window.Title = "墙体开洞";
            window.SetViewModel(viewModel);
            window.Show(); // 使用 Show() 而不是 ShowDialog()

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
