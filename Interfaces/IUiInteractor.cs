using System.Threading.Tasks;
using System.Xml;

namespace KeyappTestTask.Interfaces
{
    public interface IUiInteractor
    {
        XmlNode? FindNode(XmlDocument uiDump, string attributeValue, bool classElement = false);
        Task<XmlDocument?> TryGetUiDumpAsync();
        Task TapElementAsync(XmlDocument uiDump, string element, bool classElement = false);
        Task GoogleSearchAsync(XmlDocument uiDump, string text);
        Task FirstStartCheckAsync(XmlDocument uiDump);
        Task LaunchCromeAsync();
        Task<string> FindIpResultAsync();
    }
}
