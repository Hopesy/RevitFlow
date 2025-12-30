using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace RevitFlow.ViewModels;

public partial class WallOpeningViewModel : ObservableObject
{
    private readonly ILogger<WallOpeningViewModel> _logger;
    private ExternalEvent? _externalEvent;

    [ObservableProperty] private double _width = 1000;
    [ObservableProperty] private double _height = 2100;
    [ObservableProperty] private double _sillHeight = 0;
    [ObservableProperty] private double _radius = 500;
    [ObservableProperty] private string _shape = "rectangle";

    public WallOpeningViewModel(ILogger<WallOpeningViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 设置外部事件
    /// </summary>
    public void SetExternalEvent(ExternalEvent externalEvent)
    {
        _externalEvent = externalEvent;
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

        if (_externalEvent == null)
        {
            _logger.LogError("外部事件未初始化");
            return;
        }

        // 触发外部事件
        _externalEvent.Raise();
    }
}
