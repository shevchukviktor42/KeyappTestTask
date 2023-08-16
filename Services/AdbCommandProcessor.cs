using KeyappTestTask.Constants;
using KeyappTestTask.Interfaces;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace KeyappTestTask.Services
{
    public class AdbCommandProcessor : IAdbCommandProcessor
    {
       
        private readonly ILogger _logger;

        public AdbCommandProcessor(ILogger logger)
        {
            _logger = logger;
        }

        
        public async Task<string?> ExecuteCommandAsync(AdbCommands command, string? param = null)
        {
            string? commandText = GetCommandText(command, param);
            if (string.IsNullOrEmpty(commandText))
                return null;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = commandText,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                process.StartInfo = startInfo;
                process.Start();

                using (StreamReader reader = process.StandardOutput)
                {
                    string output = await reader.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    return output;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error executing command: {ex.Message}");
                return null;
            }
        }

        private string GetCommandText(AdbCommands command, string? param = null)

           => command switch
           {
               AdbCommands.GetRecentTasks => "shell dumpsys activity recents",
               AdbCommands.KillProces => $"shell am stack remove {param}",
               AdbCommands.KeyEvent => $"shell input keyevent {param}",
               AdbCommands.LaunchUiAutomatorDump => "exec-out uiautomator dump /dev/tty",
               AdbCommands.InputTap => $"shell input tap {param}",
               AdbCommands.LaunchUiAutomatorHelp => $"shell uiautomator help",
               AdbCommands.LaunchUiAutomatorEvents => $"shell uiautomator events",
               AdbCommands.InputText => $"shell input text {param}",
               _ => string.Empty,
           };
    }
}
