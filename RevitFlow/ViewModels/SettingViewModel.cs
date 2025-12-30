using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace RevitFlow.ViewModels;

public partial class SettingViewModel : ObservableObject
{
    private readonly ILogger<SettingViewModel> _logger;

    [ObservableProperty] private string _serverUrl = "http://localhost:8080";
    [ObservableProperty] private bool _autoConnect = true;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _messageType = "";

    public SettingViewModel(ILogger<SettingViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private void Save()
    {
        // TODO: 保存设置到配置文件
        _logger.LogInformation("保存设置: ServerUrl={Url}, AutoConnect={Auto}", ServerUrl, AutoConnect);
        Message = "设置已保存";
        MessageType = "success";
    }
}
