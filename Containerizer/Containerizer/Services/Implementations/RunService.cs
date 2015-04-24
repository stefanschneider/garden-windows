using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using IronFrame;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Containerizer.Services.Implementations
{
    public class RunService : IRunService
    {
        public IContainer container { get; set; }

        public void Run(IProcessProxy proxy, Models.ApiProcessSpec apiProcessSpec)
        {

            var processSpec = NewProcessSpec(apiProcessSpec);
            var info = container.GetInfo();
            if (info != null)
            {
                CopyExecutorEnvVariables(processSpec, info);
                CopyProcessSpecEnvVariables(processSpec, apiProcessSpec.Env);
                OverrideEnvPort(processSpec, info);
            }

            try
            {
                var process = Run(proxy, processSpec);
                proxy.SetProcessPid(process.Id);
                if (process != null)
                    WaitForExit(proxy, process);
            }
            catch (Exception ex)
            {
                proxy.ProcessExitedWithError(ex);
            }
        }

        private static void WaitForExit(IProcessProxy proxy, IContainerProcess process)
        {
            try
            {
                var exitCode = process.WaitForExit();
                proxy.ProcessExited(exitCode);
            }
            catch (Exception e)
            {
                proxy.ProcessExitedWithError(e);
            }
        }

        private IContainerProcess Run(IProcessProxy proxy, ProcessSpec processSpec)
        {
            var process = container.Run(processSpec, proxy);
            return process;
        }

        private static void OverrideEnvPort(ProcessSpec processSpec, ContainerInfo info)
        {
            if (info.ReservedPorts.Count > 0)
                processSpec.Environment["PORT"] = info.ReservedPorts[0].ToString();
        }

        private ProcessSpec NewProcessSpec(Models.ApiProcessSpec apiProcessSpec)
        {
            var processSpec = new ProcessSpec
            {
                DisablePathMapping = false,
                Privileged = false,
                WorkingDirectory = container.Directory.UserPath,
                ExecutablePath = apiProcessSpec.Path,
                Environment = new Dictionary<string, string>
                    {
                        { "ARGJSON", JsonConvert.SerializeObject(apiProcessSpec.Args) }
                    },
                Arguments = apiProcessSpec.Args
            };
            return processSpec;
        }

        private static void CopyProcessSpecEnvVariables(ProcessSpec processSpec, string[] envStrings)
        {
            if (envStrings == null) { return; }
            foreach (var kv in envStrings)
            {
                string[] arr = kv.Split(new Char[] { '=' }, 2);
                processSpec.Environment[arr[0]] = arr[1];
            }
        }

        private static void CopyExecutorEnvVariables(ProcessSpec processSpec, ContainerInfo info)
        {
            string varsJson = "";
            if (info.Properties.TryGetValue("executor:env", out varsJson))
            {
                var environmentVariables = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(varsJson);
                foreach (var dict in environmentVariables)
                {
                    processSpec.Environment[dict["name"]] = dict["value"];
                }
            }
        }

        public void Attach(IProcessProxy proxy, int pid)
        {
        }
    }
}