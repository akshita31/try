﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public delegate Task KernelCommandPipelineMiddleware(
        IKernelCommand command,
        KernelCommandContext context,
        KernelCommandPipelineContinuation next);
}