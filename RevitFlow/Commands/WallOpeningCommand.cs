using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitFlow.ViewModels;
using RevitFlow.Views;

namespace RevitFlow.Commands;

[Transaction(TransactionMode.Manual)]
public class WallOpeningCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            
            // 获取 ViewModel 并初始化
            var viewModel = Host.GetService<WallOpeningViewModel>();
            viewModel.Initialize(uiDoc);

            // 创建窗口并绑定 ViewModel
            var window = Host.GetService<WebViewWindow>();
            window.Title = "墙体开洞";
            window.SetViewModel(viewModel);
            window.ShowDialog(); // 使用 ShowDialog 阻塞等待窗口关闭

            // 窗口关闭后，执行创建洞口操作
            viewModel.ExecuteCreateOpening();
            
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
