using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RevitFlow.Services;

namespace RevitFlow.ViewModels;

public partial class WallOpeningViewModel : ObservableObject
{
    private readonly ILogger<WallOpeningViewModel> _logger;
    private readonly ExternalEvent _externalEvent;
    private readonly WallOpeningExternalEvent _handler;

    [ObservableProperty]
    private double _width = 1000;
    [ObservableProperty] 
    private double _height = 2100;
    [ObservableProperty]
    private double _sillHeight = 0;
    [ObservableProperty] 
    private double _radius = 500;
    [ObservableProperty] 
    private string _shape = "rectangle";

    public WallOpeningViewModel(ILogger<WallOpeningViewModel> logger, WallOpeningExternalEvent handler)
    {
        _logger = logger;
        _handler = handler;
        _externalEvent = ExternalEvent.Create(handler);
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
        _logger.LogInformation("触发创建洞口外部事件");

        // 更新 handler 的参数
        _handler.Width = Width;
        _handler.Height = Height;
        _handler.SillHeight = SillHeight;
        _handler.Radius = Radius;
        _handler.Shape = Shape;

        // 触发外部事件
        _externalEvent.Raise();
    }
}
