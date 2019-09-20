// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Rendering;
using Microsoft.DotNet.Interactive.Rendering.Tests;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class MagicCommandTests
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public MagicCommandTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        [Fact]
        public async Task lsmagic_lists_registered_magic_commands()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands()
                .LogEventsToPocketLogger();

            kernel.AddDirective(new Command("%%one"));
            kernel.AddDirective(new Command("%%two"));
            kernel.AddDirective(new Command("%%three"));

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode("%lsmagic"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
                  .Value
                  .ToDisplayString("text/html")
                  .Should()
                  .ContainAll("%lsmagic", "%%one", "%%three", "%%two");
        }

        [Fact]
        public async Task lsmagic_lists_registered_magic_commands_in_subkernels()
        {
            var subkernel1 = new CSharpKernel();
            subkernel1.AddDirective(new Command("%%from-subkernel-1"));
            var subkernel2 = new FSharpKernel();
            subkernel2.AddDirective(new Command("%%from-subkernel-2"));

            var compositeKernel = new CompositeKernel
                                  {
                                      subkernel1,
                                      subkernel2
                                  }
                                  .UseDefaultMagicCommands()
                                  .LogEventsToPocketLogger();

            compositeKernel.AddDirective(new Command("%%from-compositekernel"));

            using var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("%lsmagic"));

            var valueProduceds = events.OfType<DisplayedValueProduced>().ToArray();

            valueProduceds[0].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("%lsmagic",
                                         "%%csharp",
                                         "%%fsharp",
                                         "%%from-compositekernel");

            valueProduceds[1].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("%lsmagic",
                                         "%%from-subkernel-1");
            valueProduceds[2].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("%lsmagic",
                                         "%%from-subkernel-2");
        }

        [Fact]
        public async Task html_emits_string_as_content_within_a_script_element()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var html = "<b>hello!</b>";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%html\n\n{html}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            Log.Info(events.ToDisplayString());

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Equals(html));
        }

        [Fact]
        public async Task javascript_emits_string_as_content_within_a_script_element()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var scriptContent = "alert('Hello World!');";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%javascript\n{scriptContent}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            Log.Info(events.ToDisplayString());

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Equals($@"<script type=""text/javascript"">{scriptContent}</script>"));
        }


        [Fact]
        public async Task markdown_renders_markdown_content_as_html()
        {
            var kernel = new CompositeKernel()
                .UseDefaultMagicCommands();

            var expectedHtml = @"<h1 id=""topic"">Topic!</h1><p>Content</p>";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"%%markdown\n\n# Topic!\nContent"));


            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Crunch().Equals(expectedHtml));
        }

        [Fact]
        public async Task time_produces_time_elapsed_to_run_the_code_submission()
        {
            var kernel = new CompositeKernel
                {
                    new CSharpKernel().UseKernelHelpers()
                }
                .UseDefaultMagicCommands()
                .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       @"
%%time

using System.Threading.Tasks;
await Task.Delay(500);
display(""done!"");
"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced &&
                                      e.As<DisplayedValueProduced>().Value is TimeSpan)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v =>
                                     v.MimeType == "text/plain" &&
                                     v.Value.ToString().StartsWith("Wall time:") &&
                                     v.Value.ToString().EndsWith("ms"));

            events.OfType<DisplayedValueProduced>()
                  .SelectMany(e => e.FormattedValues)
                  .Should()
                  .Contain(v => v.Value.Equals("done!"));
        }

        [Fact]
        public async Task WriteFile_magic_command_writes_the_code_in_a_file()
        {
            var kernel = new CompositeKernel
                {
                    new CSharpKernel().UseKernelHelpers()
                }
                  .UseDefaultMagicCommands()
                  .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var file = Path.GetTempFileName();
            var text = "This text must be written to a file";
            await kernel.SendAsync(new SubmitCode(
                                       $@"
%%writefile {file}
{text}"));

            File.ReadAllText(file).Should().Be(text);

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v =>
                                     v.MimeType == "text/plain" &&
                                     v.Value.ToString().StartsWith($"Overwriting text to file {file}"));

        }

        [Fact]
        public async Task WriteFile_magic_command_appends_the_code_to_file_when_option_is_set()
        {
            var kernel = new CompositeKernel
                {
                    new CSharpKernel().UseKernelHelpers()
                }
                  .UseDefaultMagicCommands()
                  .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var file = Path.GetTempFileName();
            File.Create(file).Dispose();
            var existingText = "This text is already there";
            File.WriteAllText(file, existingText);

            var appendedText = "This text must be written to a file";
            await kernel.SendAsync(new SubmitCode(
$@"%%writefile -a {file}
{appendedText}"));

            File.ReadAllText(file).Should().Be(existingText + "\n" + appendedText);

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v =>
                                     v.MimeType == "text/plain" &&
                                     v.Value.ToString().StartsWith($"Appending text to file {file}"));

        }

        [Fact(Skip = "Needs https://github.com/dotnet/try/issues/469 to be resolved ")]
        public async Task WriteFile_magic_command_shows_error_if_filename_is_invalid()
        {
            var kernel = new CompositeKernel
                {
                    new CSharpKernel().UseKernelHelpers()
                }
                  .UseDefaultMagicCommands()
                  .LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var file = "INVALID_PATH";
            var text = "This text must be not be written to a file";
            await kernel.SendAsync(new SubmitCode(
                                       $@"
%%writefile {file}
{text}"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced)
                  .Which
                  .As<DisplayedValueProduced>()
                  .FormattedValues
                  .Should()
                  .ContainSingle(v =>
                                     v.MimeType == "text/plain" &&
                                     v.Value.ToString().StartsWith($"Could not write to file"));

        }
    }
}
