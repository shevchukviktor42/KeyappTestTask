using KeyappTestTask.Constants;
using KeyappTestTask.Interfaces;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace KeyappTestTask.Services
{
    public class UiInteractor : IUiInteractor
    {
        private const int _maxRetryAttempts = 3;
        private const int _delayMilliseconds = 500;
        private readonly string _urlBar = "com.android.chrome:id/url_bar";
        private readonly string _serchBox = "com.android.chrome:id/search_box_text";
        private readonly ILogger _logger;
        private readonly IAdbCommandProcessor _commandProcessor;

        public UiInteractor(ILogger logger, IAdbCommandProcessor commandProcessor)
        {
            _logger = logger;
            _commandProcessor = commandProcessor;
        }
        public async Task TapElementAsync(XmlDocument uiDump, string element, bool classElement = false)
        {
            var node = FindNode(uiDump, element, classElement);
            var buttonCoordinates = GetCoordinatesFromNode(node);
            if (buttonCoordinates != null)
            {
                var (x, y) = buttonCoordinates.Value;
                await _commandProcessor.ExecuteCommandAsync(AdbCommands.InputTap, $"{x} {y}");
            }
        }
        public async Task LaunchCromeAsync()
        {
            try
            {
                var uiDump = await TryGetUiDumpAsync();
                if (uiDump == null)
                    return;

                var chromeStr = "Chrome";
                var node = FindNode(uiDump, chromeStr);

                var coordinates = GetCoordinatesFromNode(node);
                if (coordinates == null)
                    return;

                var (x, y) = coordinates.Value;
                await _commandProcessor.ExecuteCommandAsync(AdbCommands.InputTap, $"{x} {y}");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while launching Chrome.");
            }
        }
        public async Task FirstStartCheckAsync(XmlDocument uiDump)
        {
            try
            {
                if (uiDump.InnerXml.Contains("By using Chrome, you agree to the Google Terms of Service"))
                {
                    await TapElementAsync(uiDump, "Accept & continue");
                    uiDump = await TryGetUiDumpAsync();
                    if (uiDump == null)
                        return;
                }
                if (uiDump.InnerXml.Contains("Sync your passwords, history"))
                {
                    await TapElementAsync(uiDump, "No thanks");
                    uiDump = await TryGetUiDumpAsync();
                    if (uiDump == null)
                        return;
                }
                if (uiDump.InnerXml.Contains("Chrome notifications make things easier"))
                    await TapElementAsync(uiDump, "No thanks");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred during the first start check.");
            }

        }
        public async Task GoogleSearchAsync(XmlDocument uiDump, string text)
        {
            try
            {
                var words = text.Split(' ');
                XmlNode? node = null;
                if (uiDump.InnerXml.Contains(_urlBar))
                    node = FindNode(uiDump, _urlBar, true);
                else
                    node = FindNode(uiDump, _serchBox, true);

                if (node == null)
                    return;

                var coordinates = GetCoordinatesFromNode(node);
                if (coordinates == null)
                    return;

                var (x, y) = coordinates.Value;
                await _commandProcessor.ExecuteCommandAsync(AdbCommands.InputTap, $"{x} {y}");

                foreach (var word in words)
                {
                    await _commandProcessor.ExecuteCommandAsync(AdbCommands.InputText, word);
                    await _commandProcessor.ExecuteCommandAsync(AdbCommands.KeyEvent, KeyCodes.KEYCODE_SPACE);
                }
                await _commandProcessor.ExecuteCommandAsync(AdbCommands.KeyEvent, KeyCodes.KEYCODE_ENTER);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while performing the Google search.");
            }

        }
        public async Task<string> FindIpResultAsync()
        {

            for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    var uiDump = await TryGetUiDumpAsync();

                    if (uiDump == null || !uiDump.InnerXml.Contains("Your public IP address"))
                    {
                        if (attempt < _maxRetryAttempts)
                        {
                            await Task.Delay(_delayMilliseconds);
                            continue;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }

                    var ipNode = uiDump.SelectSingleNode("//*[@text='Your public IP address']");
                    if (ipNode?.ParentNode != null)
                    {
                        var ipAttributeValue = ipNode.ParentNode.ChildNodes[0]?.Attributes["text"]?.Value;
                        if (!string.IsNullOrEmpty(ipAttributeValue))
                            return $"{ipAttributeValue}\nYour public IP address";
                    }

                    if (attempt < _maxRetryAttempts)
                    {
                        await Task.Delay(_delayMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "An error occurred while attempting to find the IP address.");
                }
            }

            return string.Empty;
        }
        public XmlNode? FindNode(XmlDocument uiDump, string attributeValue, bool classElement = false)
        {
            string searchAttribute = classElement ? "resource-id" : "text";
            var node = uiDump.SelectSingleNode($"//*[@{searchAttribute}='{attributeValue}']");
            return node;
        }
        public async Task<XmlDocument?> TryGetUiDumpAsync()
        {
            for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    string? uiDump = await _commandProcessor.ExecuteCommandAsync(AdbCommands.LaunchUiAutomatorDump).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(uiDump) && uiDump != "Killed \n")
                    {
                        uiDump = uiDump.Replace("UI hierchary dumped to: /dev/tty", "");
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(uiDump);
                        return xmlDoc;
                    }

                    if (attempt < _maxRetryAttempts)
                    {
                        await Task.Delay(_delayMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "An error occurred while attempting to get UI dump.");
                }
            }

            return null;
        }
        private (int x, int y)? GetCoordinatesFromNode(XmlNode node)
        {
            var coordinates = node.Attributes?["bounds"]?.Value ?? string.Empty;
            return string.IsNullOrEmpty(coordinates) ? null : CalculateMiddleCoordinates(coordinates);
        }
        private static (int x, int y) CalculateMiddleCoordinates(string input)
        {
            string[] parts = input.Trim('[', ']').Split("][");

            int[] leftTopCoordinates = parts[0].Split(',').Select(int.Parse).ToArray();
            int[] rightBottomCoordinates = parts[1].Split(',').Select(int.Parse).ToArray();

            int x = (leftTopCoordinates[0] + rightBottomCoordinates[0]) / 2;
            int y = (leftTopCoordinates[1] + rightBottomCoordinates[1]) / 2;
            return (x, y);
        }
    }
}
