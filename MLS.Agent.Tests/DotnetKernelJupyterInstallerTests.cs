// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using FluentAssertions;
using MLS.Agent.Tools;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MLS.Agent.Tests
{
    public class DotnetKernelJupyterInstallerTests
    {
        [Fact]
        public async Task Should_read_the_jupyter_paths_and_give_the_data_paths()
        {
            var pathsOutput = 
$@"config:
    C:\Users\akagarw\.jupyter
    C:\Users\akagarw\AppData\Local\Continuum\anaconda3\etc\jupyter
    C:\ProgramData\jupyter
data:
    C:\Users\akagarw\AppData\Roaming\jupyter
    C:\Users\akagarw\AppData\Local\Continuum\anaconda3\share\jupyter
    C:\ProgramData\jupyter
runtime:
    C:\Users\akagarw\AppData\Roaming\jupyter\runtime".Split("\n");

            DotnetKernelJupyterInstaller.ExecuteCommand executeCommand = (command, args) => Task.FromResult(new CommandLineResult(0, pathsOutput));
            var dataDirectories = await DotnetKernelJupyterInstaller.GetJupyterDataPaths(executeCommand);
            dataDirectories.Should().HaveCount(3);
        }
    }
}
