// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MLS.Agent.Tools;

namespace MLS.Agent
{
    public static class DotnetKernelJupyterInstaller
    {
        public delegate Task<CommandLineResult> ExecuteCommand(string command, string args);

        private static async Task<bool> TryInstallKernel(ExecuteCommand executeCommand)
        {
            var result = await executeCommand("jupyter", "--paths");

            

            throw new NotImplementedException();
        }

        public static async Task<IEnumerable<DirectoryInfo>> GetJupyterDataPaths(ExecuteCommand executeCommand)
        {
        /* Result of jupyter --paths
config:
    C:\Users\akagarw\.jupyter
    C:\Users\akagarw\AppData\Local\Continuum\anaconda3\etc\jupyter
    C:\ProgramData\jupyter
data:
    C:\Users\akagarw\AppData\Roaming\jupyter
    C:\Users\akagarw\AppData\Local\Continuum\anaconda3\share\jupyter
    C:\ProgramData\jupyter
runtime:
    C:\Users\akagarw\AppData\Roaming\jupyter\runtime
*/
            var result = await executeCommand("jupyter", "--paths");
            if(result.ExitCode == 0)
            {  
                string[] separator = new[] { ":" };
                var pathsDictionary = String.Join("\n", result.Output.Where(s => !string.IsNullOrWhiteSpace(s)))
                   .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                var dataPaths = pathsDictionary;
                return dataPaths.ToArray().Select(dir => new DirectoryInfo(dir));
            }

            throw new NotImplementedException();
        }
    }
}