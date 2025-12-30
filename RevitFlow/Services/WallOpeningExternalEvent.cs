using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Extensions.Logging;

namespace RevitFlow.Services;

/// <summary>
/// 墙体开洞外部事件处理器
/// </summary>
public class WallOpeningExternalEvent : IExternalEventHandler
{
    private readonly ILogger<WallOpeningExternalEvent> _logger;

    // 当前的洞口参数
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 2100;
    public double SillHeight { get; set; } = 0;
    public double Radius { get; set; } = 500;
    public string Shape { get; set; } = "rectangle";

    public WallOpeningExternalEvent(ILogger<WallOpeningExternalEvent> logger) =>  _logger = logger;
    public void Execute(UIApplication app)
    {
        var uiDocument = app.ActiveUIDocument;
        if (uiDocument == null)
        {
            _logger.LogWarning("没有活动的文档");
            return;
        }
        try
        {
            // 输出当前的参数值
            _logger.LogInformation("开始创建洞口 - Shape: {Shape}, Width: {Width}, Height: {Height}, Radius: {Radius}, SillHeight: {SillHeight}",
                Shape, Width, Height, Radius, SillHeight);

            var doc = uiDocument.Document;

            // 选择墙体
            var reference = uiDocument.Selection.PickObject(
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
            var point = uiDocument.Selection.PickPoint("请点击洞口中心位置");

            using var trans = new Transaction(doc, "创建墙体洞口");
            trans.Start();

            if (Shape == "circle")
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
        var width = Width / 304.8;
        var height = Height / 304.8;
        var sillHeight = SillHeight / 304.8;

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
        var diameter = Radius * 2 / 304.8;
        var sillHeight = SillHeight / 304.8;

        // 获取墙体的标高
        var level = doc.GetElement(wall.LevelId) as Level;
        if (level == null)
            throw new InvalidOperationException("无法获取墙体标高");

        // 计算洞口中心的 Z 坐标（相对于标高）
        var centerZ = level.Elevation + sillHeight + diameter / 2;

        // 获取墙体方向
        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve?.Curve is not Line wallLine)
            throw new InvalidOperationException("无法获取墙体位置线");

        var wallDir = wallLine.Direction;
        var halfDiameter = diameter / 2;

        // 计算洞口的两个对角点（用正方形包围圆形）
        var point1 = new XYZ(
            point.X - wallDir.X * halfDiameter,
            point.Y - wallDir.Y * halfDiameter,
            centerZ - halfDiameter
        );

        var point2 = new XYZ(
            point.X + wallDir.X * halfDiameter,
            point.Y + wallDir.Y * halfDiameter,
            centerZ + halfDiameter
        );

        // 在墙体上创建正方形洞口（近似圆形）
        doc.Create.NewOpening(wall, point1, point2);
    }

    public string GetName() => "墙体开洞外部事件";
}

public class WallSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem) => elem is Wall;
    public bool AllowReference(Reference reference, XYZ position) => true;
}
