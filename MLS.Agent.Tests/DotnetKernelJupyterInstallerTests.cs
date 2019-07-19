// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using FluentAssertions;
using MLS.Agent.Tools;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MLS.Agent.Tests
{
    public class DotnetKernelJupyterInstallerTests
    {
        [Fact]
        public async Task Should_read_the_jupyter_paths_and_give_the_data_paths()
        {
            var dataPath1 = @"C:\Users\AppData\Roaming\jupyter";
            var dataPath2 = @"C:\Users\AppData\Local\Continuum\anaconda3\share\jupyter";
            var dataPath3 = @"C:\ProgramData\jupyter";

            var pathsOutput = 
$@"config:
    C:\Users\.jupyter
data:
   {dataPath1}
   {dataPath2}
   {dataPath3}
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");

            var dataDirectories = await DotnetKernelJupyterInstaller.GetJupyterDataPaths(pathsOutput);
            dataDirectories.Should().HaveCount(3);
            dataDirectories.First().FullName.Should().Be(dataPath1);
            dataDirectories.Should().Contain(dir => dir.FullName == dataPath1);
            dataDirectories.Should().Contain(dir => dir.FullName == dataPath1);
        }
    }
}
