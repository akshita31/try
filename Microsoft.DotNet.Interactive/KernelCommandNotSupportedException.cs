﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class KernelCommandNotSupportedException : Exception
    {
        public KernelCommandNotSupportedException(IKernelCommand command, IKernel kernel)
            : base($"Command type {command} not supported by {kernel}")
        {
        }
    }
}