using WorkspaceServer;
using Xunit;
using WorkspaceServer.Tests;
using System;
using System.IO;
using FluentAssertions;
using System.Threading.Tasks;

namespace MLS.Agent.Tests
{
    public class TemplateTests
    {
        [Fact]
        public async Task When_the_template_is_installed_verify_works_for_it()
        {
            var outputDirectory = Create.EmptyWorkspace().Directory;
            var pathToTemplateCsproj = Directory.GetCurrentDirectory();
            var dotnet = new Dotnet(outputDirectory);
            await dotnet.Execute($"new -i {pathToTemplateCsproj}");
            await dotnet.New("try");

            outputDirectory.GetFiles().Should().Contain(file => file.FullName.Contains("Program.cs"));
        }
    }
}