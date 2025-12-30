using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;

namespace RevitFlow.Services;

/// <summary>
/// 墙体开洞外部事件处理器
/// </summary>
public class WallOpeningExternalEvent : IExternalEventHandler
{
    private readonly ILogger<WallOpeningExternalEvent> _logger;
    private WallOpeningViewModel? _viewModel;
    private UIDocument? _uiDocument;

    public WallOpeningExternalEvent(ILogger<WallOpeningExternalEvent> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 设置执行参数
    /// </summary>
    public void SetParameters(WallOpeningViewModel viewModel, UIDocument uiDocument)
    {
        _viewModel = viewModel;
        _uiDocument = uiDocument;
    }

    public void Execute(UIApplication app)
    {
        if (_viewModel == null || _uiDocument == null)
        {
            _logger.LogWarning("外部事件参数未设置");
            return;
        }

        try
        {
            var doc = _uiDocument.Document;

            // 选择墙体
            var reference = _uiDocument.Selection.PickObject(
                ObjectType.Element,
                new WallSelectionFilter(),
                "请选择要开洞的墙体"
            );

            if (doc.GetElement(reference) is not Wall wall)
            {
                _logger.LogWarning("选择的不是墙体");
                return;
            }

            // 选择位置
            var point = _uiDocument.Selection.PickPoint("请点击洞口中心位置");

            using var trans = new Transaction(doc, "创建墙体洞口");
            trans.Start();

            if (_viewModel.Shape == "circle")
                CreateCircularOpening(doc, wall, point);
            else
                CreateRectangularOpening(doc, wall, point);

            trans.Commit();

            _logger.LogInformation("在墙体 {WallId} 上创建洞口成功", wall.Id.Value);
            TaskDialog.Show("成功", "洞口创建成功！");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            _logger.LogInformation("用户取消了选择");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建洞口失败");
            TaskDialog.Show("错误", $"创建洞口失败：{ex.Message}");
        }
    }

    private void CreateRectangularOpening(Document doc, Wall wall, XYZ point)
    {
        var width = _viewModel!.Width / 304.8;
        var height = _viewModel.Height / 304.8;
        var sillHeight = _viewModel.SillHeight / 304.8;

        // 获取墙体的标高
        var level = doc.GetElement(wall.LevelId) as Level;
        if (level == null)
            throw new InvalidOperationException("无法获取墙体标高");

        // 计算洞口的两个对角点（相对于标高）
        var baseZ = level.Elevation + sillHeight;
        var topZ = baseZ + height;

        // 获取墙体方向
        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve?.Curve is not Line wallLine)
            throw new InvalidOperationException("无法获取墙体位置线");

        var wallDir = wallLine.Direction;
        var halfWidth = width / 2;

        // 计算洞口的两个对角点（沿墙体方向）
        var point1 = new XYZ(
            point.X - wallDir.X * halfWidth,
            point.Y - wallDir.Y * halfWidth,
            baseZ
        );

        var point2 = new XYZ(
            point.X + wallDir.X * halfWidth,
            point.Y + wallDir.Y * halfWidth,
            topZ
        );

        // 在墙体上创建矩形洞口
        doc.Create.NewOpening(wall, point1, point2);
    }

    private void CreateCircularOpening(Document doc, Wall wall, XYZ point)
    {
        var radius = _viewModel!.Radius / 304.8;
        var sillHeight = _viewModel.SillHeight / 304.8;

        // 获取墙体的标高
        var level = doc.GetElement(wall.LevelId) as Level;
        if (level == null)
            throw new InvalidOperationException("无法获取墙体标高");

        // 计算圆心位置（相对于标高）
        var centerZ = level.Elevation + sillHeight + radius;
        var center = new XYZ(point.X, point.Y, centerZ);

        // 获取墙体方向
        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve?.Curve is not Line wallLine)
            throw new InvalidOperationException("无法获取墙体位置线");

        var wallDir = wallLine.Direction;

        // 创建圆形轮廓
        var curveArray = new CurveArray();
        curveArray.Append(Arc.Create(center, radius, 0, Math.PI, wallDir, XYZ.BasisZ));
        curveArray.Append(Arc.Create(center, radius, Math.PI, 2 * Math.PI, wallDir, XYZ.BasisZ));

        // 在墙体上创建圆形洞口
        doc.Create.NewOpening(wall, curveArray, true);
    }

    public string GetName() => "墙体开洞外部事件";
}

public class WallSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem) => elem is Wall;
    public bool AllowReference(Reference reference, XYZ position) => true;
}
