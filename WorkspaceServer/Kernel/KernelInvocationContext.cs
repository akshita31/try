﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public class KernelInvocationContext : IObserver<IKernelEvent>
    {
        private readonly KernelCommandInvocation _invocation;
        private readonly Action<IKernelEvent> _publishEvent;
        private readonly ReplaySubject<IKernelEvent> _events = new ReplaySubject<IKernelEvent>();

        public KernelInvocationContext(
            KernelCommandInvocation invocation,
            Action<IKernelEvent> publishEvent,
            CancellationToken cancellationToken)
        {
            _invocation = invocation;
            _publishEvent = publishEvent;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public void OnCompleted()
        {
            _events.OnCompleted();
        }

        public void OnError(Exception exception)
        {
            _events.OnError(exception);
        }

        public void OnNext(IKernelEvent @event)
        {
            _events.OnNext(@event);
            _publishEvent(@event);
        }

        internal IObservable<IKernelEvent> KernelEvents => _events;

        public async Task InvokeAsync()
        {
            await _invocation(this);
        }
    }
}