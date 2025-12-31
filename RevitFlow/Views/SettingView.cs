using Microsoft.Extensions.Logging;
using RevitFlow.ViewModels;

namespace RevitFlow.Views;
// 设置窗口
// 子类没有 XAML，只定制行为
public  class SettingView : WebViewBase
{
    public SettingView(ILogger<WebViewBase> logger, SettingViewModel viewModel) : base(logger, viewModel)
    { 
        // 不调用 InitializeComponent()
        // 使用父类的 XAML
        SetPageName("index.html?page=setting");
    }
}
/* xaml没有继承机制，会完全覆盖掉原有的ui，所以只写了cs文件
 * <!-- SettingView.xaml -->
  <local:WebViewBase x:Class="RevitFlow.Views.SettingView" ...>
      <!-- 这里需要重新定义所有 UI -->
      <Grid>
          <TextBlock x:Name="LoadingText" ... />
          <wv2:WebView2 x:Name="WebView" ... />
      </Grid>
  </local:WebViewBase>
 */
