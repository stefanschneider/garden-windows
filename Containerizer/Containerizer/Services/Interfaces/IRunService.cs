using Containerizer.Controllers;
using Containerizer.Models;
using IronFrame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IRunService
    {
        IContainer container { get; set; }

        void Run(IProcessProxy processIO, ApiProcessSpec processSpec);
    }
}
