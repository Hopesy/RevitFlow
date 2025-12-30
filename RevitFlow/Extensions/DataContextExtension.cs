using System.Reflection;
using Autodesk.Revit.UI;

namespace RevitFlow.Extensions;

public static class DataContextExtension
{
    public static UIApplication GetUIApplication(this UIControlledApplication application)
    {
        var type = typeof(UIControlledApplication);
        var propertie = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(e => e.FieldType == typeof(UIApplication));
        return propertie?.GetValue(application) as UIApplication;
    }
}