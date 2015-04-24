using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IronFrame;

namespace Containerizer.Services.Interfaces
{
    public interface IProcessProxy : IProcessIO
    {
        void SetProcessPid(int pid);
        void ProcessExited(int exitCode);
        void ProcessExitedWithError(Exception ex);
    }
}
