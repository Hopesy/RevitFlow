using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitFlow.Views;

namespace RevitFlow.Commands;

[Transaction(TransactionMode.Manual)]
public class WallOpeningCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            // 从容器获取窗口（所有依赖自动注入）
            var window = Host.GetService<WallOpeningWindow>();
            window.Title = "墙体开洞";
            window.Show();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
