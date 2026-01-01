using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Extensions.Logging;

namespace RevitFlow.Services;

/// <summary>
/// 曲线阵列族外部事件处理器
/// </summary>
public class CurveArrayExternalEvent : IExternalEventHandler
{
    private readonly ILogger<CurveArrayExternalEvent> _logger;

    // 当前的阵列参数
    public string SelectedFamilyName { get; set; } = "";
    public int Count { get; set; } = 10;
    public bool AlignToPath { get; set; } = true;

    public CurveArrayExternalEvent(ILogger<CurveArrayExternalEvent> logger) => _logger = logger;

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
            _logger.LogInformation("开始曲线阵列 - Family: {Family}, Count: {Count}, AlignToPath: {Align}",
                SelectedFamilyName, Count, AlignToPath);

            var doc = uiDocument.Document;

            // 根据前端选择的族类型名称查找 FamilySymbol
            var familySymbol = FindFamilySymbol(doc, SelectedFamilyName);
            if (familySymbol == null)
            {
                _logger.LogWarning("未找到族类型: {FamilyName}", SelectedFamilyName);
                TaskDialog.Show("错误", $"未找到族类型：{SelectedFamilyName}");
                return;
            }

            // 选择模型线
            var curveRef = uiDocument.Selection.PickObject(
                ObjectType.Element,
                new ModelCurveSelectionFilter(),
                "请选择模型线");

            if (doc.GetElement(curveRef) is not ModelCurve modelCurve)
            {
                _logger.LogWarning("选择的不是模型线");
                return;
            }

            using var trans = new Transaction(doc, "曲线阵列族");
            trans.Start();

            ArrayAlongCurve(doc, modelCurve, familySymbol);

            trans.Commit();

            _logger.LogInformation("曲线阵列完成");
            TaskDialog.Show("成功", $"已沿曲线阵列 {Count} 个族实例！");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            _logger.LogInformation("用户取消了选择");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "曲线阵列失败");
            TaskDialog.Show("错误", $"曲线阵列失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据族类型名称查找 FamilySymbol
    /// </summary>
    private FamilySymbol? FindFamilySymbol(Document doc, string familyTypeName)
    {
        // 格式: "族名称 : 类型名称"
        var parts = familyTypeName.Split(" : ");
        if (parts.Length != 2)
        {
            _logger.LogWarning("族类型名称格式错误: {Name}", familyTypeName);
            return null;
        }

        var familyName = parts[0].Trim();
        var typeName = parts[1].Trim();

        var familySymbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(fs => fs.Family.Name == familyName && fs.Name == typeName);

        return familySymbol;
    }

    /// <summary>
    /// 沿曲线阵列族实例
    /// </summary>
    private void ArrayAlongCurve(Document doc, ModelCurve modelCurve, FamilySymbol familySymbol)
    {
        var curve = modelCurve.GeometryCurve;

        // 确保族类型已激活
        if (!familySymbol.IsActive)
        {
            familySymbol.Activate();
        }

        // 沿曲线创建实例
        for (int i = 0; i < Count; i++)
        {
            double parameter = Count == 1 ? 0.5 : (double)i / (Count - 1);
            var point = curve.Evaluate(parameter, true);

            // 创建新实例
            var newInstance = doc.Create.NewFamilyInstance(
                point,
                familySymbol,
                Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            // 如果需要对齐到路径，计算旋转角度
            if (AlignToPath)
            {
                var tangent = curve.ComputeDerivatives(parameter, true).BasisX;
                var angle = Math.Atan2(tangent.Y, tangent.X);

                // 旋转实例
                var axis = Line.CreateBound(point, point + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(doc, newInstance.Id, axis, angle);
            }
        }
    }

    public string GetName() => "曲线阵列族外部事件";
}

public class ModelCurveSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem) => elem is ModelCurve;
    public bool AllowReference(Reference reference, XYZ position) => true;
}
