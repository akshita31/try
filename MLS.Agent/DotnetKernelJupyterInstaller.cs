// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MLS.Agent.Tools;

namespace MLS.Agent
{
    public static class DotnetKernelJupyterInstaller
    {
        public delegate Task<CommandLineResult> ExecuteCommand(string command, string args);

        private static async Task<int> TryInstallKernel(ExecuteCommand executeCommand, IConsole console)
        {
            try
            {
                var result = await executeCommand("jupyter", "--paths");
                if(result.ExitCode ==0)
                {
                    var dataDirectories = await GetJupyterDataPaths(result.Output);
                    Installkernel(dataDirectories);
                    console.Out.WriteLine(".NET kernel installation succeded");
                    return 0;
                }
                else
                {
                    throw new KernelInstallationFailureException($"Tried to invoke \"jupyter --paths\" but failed with exception: {string.Join("\n", result.Error)}");
                }
            }
            catch (KernelInstallationFailureException e)
            {
                console.Out.WriteLine($".NET Kernel Installation failed with error {string.Join("\n", e.Message)}");
                return -1;
            }
        }

        private static void Installkernel(IEnumerable<DirectoryInfo> dataDirectories)
        {
            throw new NotImplementedException();
        }

        public static async Task<IEnumerable<DirectoryInfo>> GetJupyterDataPaths(IReadOnlyCollection<string> jupyterPathInfo)
        {
            var indexOfDataNode = Array.FindIndex(jupyterPathInfo.ToArray(), element => element.TrimLineEndings().CompareTo("data:") == 0);
            if (indexOfDataNode != -1)
            {
                var currentPaths = new List<string>();
                for (var index = indexOfDataNode + 1; index < jupyterPathInfo.Count; index++)
                {
                    string element = jupyterPathInfo.ElementAt(index);
                    if (!string.IsNullOrWhiteSpace(element))
                    {
                        if (element.Trim('\r', '\n').EndsWith(":"))
                        {
                            break;
                        }
                        else
                        {
                            currentPaths.Add(element);
                        }
                    }
                }

                return currentPaths.Select(dir => new DirectoryInfo(dir));
            }
            else
            {
                throw new KernelInstallationFailureException($"Could not find the jupyter kernel installation directory." +
                    $" Output of \"jupyter --paths\" is {string.Join('\n', jupyterPathInfo)}");
            }
        }
    }

    [Serializable]
    internal class KernelInstallationFailureException : Exception
    {
        public KernelInstallationFailureException(string message) : base(message)
        {
        }
    }
}