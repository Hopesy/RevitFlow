using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RevitFlow.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitFlow.ViewModels;

public partial class CurveArrayViewModel : ObservableObject
{
    private readonly ILogger<CurveArrayViewModel> _logger;
    private readonly ExternalEvent _externalEvent;
    private readonly CurveArrayExternalEvent _handler;

    [ObservableProperty] private string _selectedFamilyName = "";
    [ObservableProperty] private int _count = 10;
    [ObservableProperty] private bool _alignToPath = true;

    public CurveArrayViewModel( ILogger<CurveArrayViewModel> logger, CurveArrayExternalEvent handler)
    {
        _logger = logger;
        _handler = handler;
        _externalEvent = ExternalEvent.Create(handler);
    }

    [RelayCommand]
    private void CreateArray()
    {
        _logger.LogInformation("触发曲线阵列外部事件");
        // 更新 handler 的参数
        _handler.SelectedFamilyName = SelectedFamilyName;
        _handler.Count = Count;
        _handler.AlignToPath = AlignToPath;
        // 触发外部事件
        _externalEvent.Raise();
    }

    /// <summary>
    /// 获取项目中的所有常规模型族类型（由 Command 调用）
    /// </summary>
    public static List<string> GetFamilyTypes(Document doc)
    {
        var familyTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .Where(fs => fs.Family.FamilyCategory != null)
            .Where(fs => fs.Family.FamilyCategory.CategoryType == CategoryType.Model)
            // 只显示常规模型类别
            .Where(fs => fs.Family.FamilyCategory.BuiltInCategory == BuiltInCategory.OST_GenericModel)
            .Select(fs => $"{fs.Family.Name} : {fs.Name}")
            .Distinct()
            .OrderBy(name => name)
            .ToList();
        return familyTypes;
    }
}
