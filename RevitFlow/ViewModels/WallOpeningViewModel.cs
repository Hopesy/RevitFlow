using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace RevitFlow.ViewModels;

public partial class WallOpeningViewModel : ObservableObject
{
    private readonly ILogger<WallOpeningViewModel> _logger;
    private UIDocument? _uiDocument;

    [ObservableProperty] private double _width = 1000;
    [ObservableProperty] private double _height = 2100;
    [ObservableProperty] private double _sillHeight = 0;
    [ObservableProperty] private double _radius = 500;
    [ObservableProperty] private string _shape = "rectangle";

    /// <summary>
    /// 是否确认创建（点击了创建按钮）
    /// </summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>
    /// 请求关闭窗口的事件
    /// </summary>
    public event Action? RequestClose;

    public WallOpeningViewModel(ILogger<WallOpeningViewModel> logger)
    {
        _logger = logger;
    }

    public void Initialize(UIDocument uiDoc)
    {
        _uiDocument = uiDoc;
        IsConfirmed = false;
    }

    [RelayCommand]
    private void ApplyPreset(string preset)
    {
        switch (preset)
        {
            case "door":
                Width = 900; Height = 2100; SillHeight = 0; Shape = "rectangle";
                break;
            case "window":
                Width = 1500; Height = 1500; SillHeight = 900; Shape = "rectangle";
                break;
            case "circle":
                Radius = 300; SillHeight = 1000; Shape = "circle";
                break;
        }
    }

    [RelayCommand]
    private void CreateOpening()
    {
        IsConfirmed = true;
        RequestClose?.Invoke();
    }

    /// <summary>
    /// 执行墙体选择和洞口创建 (在窗口关闭后由 Command 调用)
    /// </summary>
    public void ExecuteCreateOpening()
    {
        if (_uiDocument == null || !IsConfirmed) return;

        try
        {
            var doc = _uiDocument.Document;

            // 选择墙体
            var reference = _uiDocument.Selection.PickObject(
                ObjectType.Element,
                new WallSelectionFilter(),
                "请选择要开洞的墙体"
            );

            if (doc.GetElement(reference) is not Wall wall) return;

            // 选择位置
            var point = _uiDocument.Selection.PickPoint("请点击洞口中心位置");

            using var trans = new Transaction(doc, "创建墙体洞口");
            trans.Start();

            if (Shape == "circle")
                CreateCircularOpening(doc, wall, point);
            else
                CreateRectangularOpening(doc, wall, point);

            trans.Commit();

            _logger.LogInformation("在墙体 {WallId} 上创建洞口成功", wall.Id.Value);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            _logger.LogInformation("用户取消了选择");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建洞口失败");
        }
    }

    private void CreateRectangularOpening(Document doc, Wall wall, XYZ point)
    {
        var width = Width / 304.8;
        var height = Height / 304.8;
        var sillHeight = SillHeight / 304.8;

        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve?.Curve is not Line wallLine)
            throw new InvalidOperationException("无法获取墙体位置线");

        var wallDir = wallLine.Direction;
        var halfWidth = width / 2;
        var baseZ = point.Z + sillHeight;

        var p1 = new XYZ(point.X - wallDir.X * halfWidth, point.Y - wallDir.Y * halfWidth, baseZ);
        var p2 = new XYZ(point.X + wallDir.X * halfWidth, point.Y + wallDir.Y * halfWidth, baseZ);
        var p3 = new XYZ(p2.X, p2.Y, baseZ + height);
        var p4 = new XYZ(p1.X, p1.Y, baseZ + height);

        var curveArray = new CurveArray();
        curveArray.Append(Line.CreateBound(p1, p2));
        curveArray.Append(Line.CreateBound(p2, p3));
        curveArray.Append(Line.CreateBound(p3, p4));
        curveArray.Append(Line.CreateBound(p4, p1));

        doc.Create.NewOpening(wall, curveArray, true);
    }

    private void CreateCircularOpening(Document doc, Wall wall, XYZ point)
    {
        var radius = Radius / 304.8;
        var sillHeight = SillHeight / 304.8;

        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve?.Curve is not Line wallLine)
            throw new InvalidOperationException("无法获取墙体位置线");

        var wallDir = wallLine.Direction;
        var centerZ = point.Z + sillHeight + radius;
        var center = new XYZ(point.X, point.Y, centerZ);

        var curveArray = new CurveArray();
        curveArray.Append(Arc.Create(center, radius, 0, Math.PI, wallDir, XYZ.BasisZ));
        curveArray.Append(Arc.Create(center, radius, Math.PI, 2 * Math.PI, wallDir, XYZ.BasisZ));

        doc.Create.NewOpening(wall, curveArray, true);
    }
}

public class WallSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem) => elem is Wall;
    public bool AllowReference(Reference reference, XYZ position) => true;
}
