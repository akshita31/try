using WorkspaceServer;
using Xunit;
using WorkspaceServer.Tests;
using System;
using System.IO;
using FluentAssertions;
using System.Threading.Tasks;
using System.CommandLine;
using MLS.Agent.CommandLine;
using Microsoft.DotNet.Try.Protocol.Tests;
using System.Linq;

namespace MLS.Agent.Tests
{
    public class TemplateTests: IDisposable
    {
        private string _pathToTemplateCsproj;

        public TemplateTests()
        {
            _pathToTemplateCsproj = Path.Combine(Directory.GetCurrentDirectory(), "template");
            var dotnet = new Dotnet();
            Task.Run(() => dotnet.Execute($"new -i {_pathToTemplateCsproj}")).Wait();
        }

        public void Dispose()
        {
            var dotnet = new Dotnet();
            Task.Run(() => dotnet.Execute($"new --uninstall {_pathToTemplateCsproj}")).Wait();
        }

        [Fact]
        public async Task When_the_template_is_installed_it_has_the_files()
        {
            var outputDirectory = Create.EmptyWorkspace().Directory;
            var dotnet = new Dotnet(outputDirectory);
            await dotnet.New("try");
            outputDirectory.GetFiles().Should().Contain(file => file.FullName.Contains("Program.cs"));
            outputDirectory.GetFiles().Should().Contain(file => file.FullName.Contains("Readme.md"));
        }

        [Fact]
        public async Task When_the_template_is_installed_verify_works()
        {
            var outputDirectory = Create.EmptyWorkspace().Directory;
            var dotnet = new Dotnet(outputDirectory);
            await dotnet.New("try");

            var console = new TestConsole();
            var directoryAccessor = new FileSystemDirectoryAccessor(outputDirectory);

            var resultCode = await VerifyCommand.Do(
                new VerifyOptions(outputDirectory),
                console,
                () => directoryAccessor,
                PackageRegistry.CreateForTryMode(outputDirectory));

            resultCode.Should().Be(0);
            console.Out
                       .ToString()
                       .EnforceLF()
                       .Trim()
                       .Should()
                       .Match(
                           $"{outputDirectory}{Path.DirectorySeparatorChar}Readme.md*Line 7:*{outputDirectory}{Path.DirectorySeparatorChar}Program.cs (in project {outputDirectory}{Path.DirectorySeparatorChar}Microsoft.DotNet.Try.Template.csproj)*".EnforceLF());

        }

        [Fact]
        public async Task The_installed_project_has_the_name_of_the_folder()
        {
            var outputDirectory = Create.EmptyWorkspace().Directory;
            var dotnet = new Dotnet(outputDirectory);
            await dotnet.New("try");

            outputDirectory.GetFiles("*.csproj").Single().Name.Should().Contain(outputDirectory.Name);
        }

        [Fact]
        public async Task When_the_name_argument_is_passed_it_creates_a_folder_with_the_project_having_the_passed_name()
        {
            var outputDirectory = Create.EmptyWorkspace().Directory;
            var dotnet = new Dotnet(outputDirectory);
            await dotnet.New("try --name testProject");

            outputDirectory.GetDirectories().Single().GetFiles("*.csproj").Single().Name.Should().Be("testProject.csproj");
        }
    }
}