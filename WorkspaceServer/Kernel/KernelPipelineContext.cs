﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class KernelPipelineContext
    {
        private readonly Action<IKernelEvent> _publishEvent;
        private readonly List<KernelInvocationContext> _invocations = new List<KernelInvocationContext>();

        public KernelPipelineContext(Action<IKernelEvent> publishEvent)
        {
            _publishEvent = publishEvent;
        }

        internal IKernel Kernel { get; set; }

        public void OnExecute(KernelCommandInvocation invocation)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            _invocations.Add(new KernelInvocationContext(
                                 invocation,
                                 _publishEvent));
        }

        internal async Task<IKernelCommandResult> InvokeAsync()
        {
            var invocationContexts = _invocations.ToArray();

            var observable = invocationContexts.Select(i => i.KernelEvents).Merge();

            foreach (var invocation in invocationContexts)
            {
                await invocation.InvokeAsync();
            }

            return new KernelCommandResult(observable);
        }
    }
}