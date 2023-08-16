using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeyappTestTask.Interfaces
{
    public interface IAppManager
    {
        Task KillAllAppsAsync();
        Task LaunchChromeFromDesktopAsync(string searchText);
    }
}
