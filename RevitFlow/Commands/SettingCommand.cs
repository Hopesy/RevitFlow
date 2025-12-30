using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitFlow.Views;

namespace RevitFlow.Commands;

[Transaction(TransactionMode.Manual)]
public class SettingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            // 从容器获取 Window（自动注入 ViewModel）
            var window = Host.GetService<SettingView>();
            window.Title = "RevitFlow 设置";
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
