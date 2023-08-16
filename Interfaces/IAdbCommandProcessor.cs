using KeyappTestTask.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyappTestTask.Interfaces
{
    public interface IAdbCommandProcessor
    {
        Task<string?> ExecuteCommandAsync(AdbCommands command, string? param = null);
    }
}
