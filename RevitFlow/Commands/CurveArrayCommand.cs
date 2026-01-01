using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;
using RevitFlow.Views;

namespace RevitFlow.Commands;

[Transaction(TransactionMode.Manual)]
public class CurveArrayCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var logger = Host.GetService<ILogger<CurveArrayCommand>>();
            // 获取当前文档的族列表
            var doc = commandData.Application.ActiveUIDocument.Document;
            var familyTypes = CurveArrayViewModel.GetFamilyTypes(doc);
            // 从容器获取窗口
            var window = Host.GetService<CurveArrayView>();
            window.Title = "曲线阵列族";
            // 先注册 Loaded 事件，再显示窗口
            window.Loaded += async (s, e) =>
            {
                logger.LogInformation("窗口 Loaded 事件触发");
                // 延迟确保 WebView2 完全加载
                await System.Threading.Tasks.Task.Delay(1000);
                logger.LogInformation("准备发送族列表到 JavaScript，共 {Count} 个", familyTypes.Count);
                await window.SendToJavaScriptAsync("onFamilyListReceived", familyTypes);
            };
            // 显示窗口
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
