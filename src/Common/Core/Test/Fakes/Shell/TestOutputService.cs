﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core.Logging;
using Microsoft.Common.Core.Services;
using Microsoft.R.Common.Core.Output;

namespace Microsoft.Common.Core.Test.Fakes.Shell {
    public class TestOutputService : IOutputService {
        private readonly IServiceContainer _services;
        private readonly ConcurrentDictionary<string, IOutput> _outputs;

        public TestOutputService(IServiceContainer services) {
            _services = services;
            _outputs = new ConcurrentDictionary<string, IOutput>();
        }

        public Task<IOutput> GetAsync(string name, CancellationToken cancellationToken) 
            => Task.FromResult(_outputs.GetOrAdd(name, prefix => new TestOutput(prefix, _services.Log())));

        private class TestOutput : IOutput {
            private readonly string _prefix;
            private readonly IActionLog _log;

            public TestOutput(string prefix, IActionLog log) {
                _prefix = prefix;
                _log = log;
            }

            public void Write(string text) {
                _log.Write(LogVerbosity.Minimal, MessageCategory.General, $"[{_prefix} output]: {text}");
            }
        }
    }
}