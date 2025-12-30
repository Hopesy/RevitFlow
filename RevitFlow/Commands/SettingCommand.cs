using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitFlow.ViewModels;
using RevitFlow.Views;

namespace RevitFlow.Commands;

[Transaction(TransactionMode.Manual)]
public class SettingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var viewModel = Host.GetService<SettingViewModel>();
            var window = Host.GetService<WebViewWindow>();
            window.Title = "RevitFlow 设置";
            window.SetViewModel(viewModel, "setting.html");
            window.ShowDialog();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
