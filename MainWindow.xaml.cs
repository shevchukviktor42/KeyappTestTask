using KeyappTestTask.Interfaces;
using KeyappTestTask.Services;
using NLog;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KeyappTestTask
{

    public partial class MainWindow : Window
    {
        private readonly string _myIpStr = "my ip address";
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IAdbCommandProcessor _commandProcessor;
        private readonly IAppManager _appManager;
        private readonly IUiInteractor _uiInteractor;
        public MainWindow()
        {
            InitializeComponent();
            _commandProcessor = new AdbCommandProcessor(_logger);
            _uiInteractor = new UiInteractor(_logger, _commandProcessor);
            _appManager = new AppManager(_logger, _commandProcessor, _uiInteractor);
            _logger.Error("started");
        }
        private async void CloseAppsButton_Click(object sender, RoutedEventArgs e)
          => await _appManager.KillAllAppsAsync();

        private async void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                await PerformSearchAsync();
                e.Handled = true;
            }
        }
        private async void Search_Click(object sender, RoutedEventArgs e)
        => await PerformSearchAsync();

        private async Task PerformSearchAsync()
        {
            string searchText = searchTextBox.Text;
            if (!string.IsNullOrEmpty(searchText))
            {

                resultTextBlock.Text = string.Empty;
                await _appManager.LaunchChromeFromDesktopAsync(searchText);
                if (searchText == _myIpStr)
                {
                    var ip = await _uiInteractor.FindIpResultAsync();
                    resultTextBlock.Text = ip;
                }
            }
        }
    }
}
