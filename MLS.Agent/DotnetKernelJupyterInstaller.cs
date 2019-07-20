// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MLS.Agent.Tools;

namespace MLS.Agent.Jupyter
{
    public static class DotnetKernelJupyterInstaller
    {
        public delegate Task<CommandLineResult> ExecuteCommand(string command, string args);

        public static async Task<int> InstallKernel(ExecuteCommand executeCommand, IConsole console)
        {
            var dataPathsResult = JupyterPathInfo.GetDataPaths(await executeCommand("jupyter", "--paths"));
            if (string.IsNullOrEmpty(dataPathsResult.Error))
            {
                Installkernel(dataPathsResult.Paths, console);
                console.Out.WriteLine(".NET kernel installation succeded");
                return 0;
            }
            else
            {
                console.Error.WriteLine($".NET Kernel Installation failed with error: {dataPathsResult.Error}");
                return -1;
            }
        }

        private static void Installkernel(IEnumerable<DirectoryInfo> dataDirectories, IConsole console)
        {
            foreach (var directory in dataDirectories)
            {
                if (directory.Exists)
                {
                    var kernelDirectory = directory.Subdirectory("kernels");
                    if (!kernelDirectory.Exists)
                    {
                        kernelDirectory.Create();
                    }

                    var dotnetkernelDir = kernelDirectory.Subdirectory(".NET");
                    if (!dotnetkernelDir.Exists)
                    {
                        dotnetkernelDir.Create();
                    }

                    console.Out.WriteLine($"Installing the .NET kernel in directory: {dotnetkernelDir.FullName}");

                    // Copy the files into the kernels directory
                    File.Copy("kernels.json", Path.Combine(dotnetkernelDir.FullName, "kernels.json"));
                    File.Copy("logo-32x32.png", Path.Combine(dotnetkernelDir.FullName, "kernels.json"));
                    File.Copy("logo-64x64.png", Path.Combine(dotnetkernelDir.FullName, "kernels.json"));
                    console.Out.WriteLine($"Finished installing the .NET kernel in directory: {dotnetkernelDir.FullName}");
                }
            }
        }
    }
}