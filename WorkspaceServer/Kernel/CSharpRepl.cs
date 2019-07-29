﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace WorkspaceServer.Kernel
{
    public class CSharpRepl : IKernel
    {
        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Subject<IKernelEvent> _channel;
        private ScriptState _scriptState;

        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        protected ScriptOptions ScriptOptions;

        protected StringBuilder _inputBuffer = new StringBuilder();

        public IObservable<IKernelEvent> KernelEvents => _channel;

        public CSharpRepl()
        {
            _channel = new Subject<IKernelEvent>();
            SetupScriptOptions();
        }

        private void SetupScriptOptions()
        {
            ScriptOptions = ScriptOptions.Default
                .AddImports(
                    "System",
                    "System.Text",
                    "System.Collections",
                    "System.Collections.Generic",
                    "System.Threading.Tasks",
                    "System.Linq")
                .AddReferences(
                    typeof(Enumerable).GetTypeInfo().Assembly,
                    typeof(IEnumerable<>).GetTypeInfo().Assembly,
                    typeof(Task<>).GetTypeInfo().Assembly);
        }

        public async Task SendAsync(SubmitCode submitCode, CancellationToken cancellationToken)
        {
            _channel.OnNext(new CodeSubmissionReceived(submitCode.Id, submitCode.Value));

            var (shouldExecute, code) = ComputeFullSubmission(submitCode.Value);

            if (shouldExecute)
            {
                _channel.OnNext(new CompleteCodeSubmissionReceived(submitCode.Id));
                Exception exception = null;
                try
                {
                    if (_scriptState == null)
                    {
                        _scriptState = await CSharpScript.RunAsync(
                            code, 
                            ScriptOptions, 
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        _scriptState = await _scriptState.ContinueWithAsync(
                            code, 
                            ScriptOptions, 
                            e =>
                            {
                                exception = e;
                                return true;
                            },
                            cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                var hasReturnValue = _scriptState != null && (bool)_hasReturnValueMethod.Invoke(_scriptState.Script, null);

                if (hasReturnValue)
                {
                    _channel.OnNext(new ValueProduced(submitCode.Id, _scriptState.ReturnValue));
                }
                if (exception != null)
                {
                    var diagnostics = _scriptState?.Script?.GetDiagnostics() ?? Enumerable.Empty<Diagnostic>();
                    if (diagnostics.Any())
                    {
                        var message = string.Join("\n", diagnostics.Select(d => d.GetMessage()));

                        _channel.OnNext(new CodeSubmissionEvaluationFailed(submitCode.Id, exception, message));
                    }
                    else
                    {
                        _channel.OnNext(new CodeSubmissionEvaluationFailed(submitCode.Id, exception));
                    }
                }
                else
                {
                    _channel.OnNext(new CodeSubmissionEvaluated(submitCode.Id));
                }
            }
            else
            {
                _channel.OnNext(new IncompleteCodeSubmissionReceived(submitCode.Id));
            }
        }

        private (bool shouldExecute, string completeSubmission) ComputeFullSubmission(string input)
        {
            _inputBuffer.AppendLine(input);

            var code = _inputBuffer.ToString();
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, ParseOptions);

            if (!SyntaxFactory.IsCompleteSubmission(syntaxTree))
            {
                return (false, code);
            }

            _inputBuffer = new StringBuilder();
            return (true, code);
        }

        public Task SendAsync(IKernelCommand command)
        {
            return SendAsync(command, CancellationToken.None);
        }

        public Task SendAsync(IKernelCommand command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    return SendAsync(submitCode, cancellationToken);

                default:
                    throw new KernelCommandNotSupportedException(command, this);
            }
        }
    }

    internal static class EnumerableExtensions
    {
        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            var comparer = Comparer<T>.Create(compare);
            return source.OrderBy(t => t, comparer);
        }
    }

    internal static class ScriptExtensions
    {
        public static IEnumerable<Diagnostic> GetDiagnostics(this Script script)
        {
            if(script == null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var compilation = script.GetCompilation();
            var orderedDiagnostics = compilation.GetDiagnostics().OrderBy((d1, d2) =>
            {
                var severityDiff = (int)d2.Severity - (int)d1.Severity;
                return severityDiff != 0 ? severityDiff : d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start;
            });

            return orderedDiagnostics;
        }
    }
}