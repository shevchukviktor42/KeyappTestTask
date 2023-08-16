using KeyappTestTask.Constants;
using KeyappTestTask.Interfaces;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeyappTestTask.Services
{
    public class AppManager : IAppManager
    {
        private readonly string _urlBar = "com.android.chrome:id/url_bar";
        private readonly string _serchBox = "com.android.chrome:id/search_box_text";
        private readonly ILogger _logger;
        private readonly IAdbCommandProcessor _commandProcessor;
        private readonly IUiInteractor _uiInteractor;

        public AppManager(ILogger logger, IAdbCommandProcessor commandProcessor, IUiInteractor uiInteractor)
        {
            _logger = logger;
            _commandProcessor = commandProcessor;
            _uiInteractor = uiInteractor;
        }
        public async Task KillAllAppsAsync()
        {
            try
            {
                var procesListOutput = await _commandProcessor.ExecuteCommandAsync(AdbCommands.GetRecentTasks);
                if (procesListOutput == null)
                    return;

                var appList = GetRecentsApps(procesListOutput);

                if (appList.Count == 0)

                    return;

                foreach (var app in appList)
                {
                    _logger.Info($"Killing app: {app}");
                    await _commandProcessor.ExecuteCommandAsync(AdbCommands.KillProces, app);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while killing apps.");
            }

        }
        public async Task LaunchChromeFromDesktopAsync(string searchText)
        {
            try
            {

                await _commandProcessor.ExecuteCommandAsync(AdbCommands.KeyEvent, KeyCodes.KEYCODE_HOME);

                await _uiInteractor.LaunchCromeAsync();

                var uiDump = await _uiInteractor.TryGetUiDumpAsync();
                if (uiDump == null)

                    return;


                if (!uiDump.InnerXml.Contains(_urlBar) && !uiDump.InnerXml.Contains(_serchBox))
                {
                    await _uiInteractor.FirstStartCheckAsync(uiDump);
                    uiDump = await _uiInteractor.TryGetUiDumpAsync();
                }


                await _uiInteractor.GoogleSearchAsync(uiDump, searchText);

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while launching Chrome.");
            }

        }
        private List<string> GetRecentsApps(string output)
        {
            var appList = new List<string>();

            var splitedOutput = output.Split("Recent #").Where(x => x.Contains("type=standard"));

            foreach (var line in splitedOutput)
                appList.Add(line.Split("type")[0].Split('#')[1]);

            return appList;
        }
    }


}
