﻿// Copyright 2020, Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using System;

namespace Google.Cloud.Functions.Invoker.Logging
{
    /// <summary>
    /// Base class for loggers that don't do any of their own filtering, and don't support scopes.
    /// </summary>
    internal abstract class LoggerBase : ILogger
    {
        protected string Category { get; }

        protected LoggerBase(string category) =>
            Category = category;

        // We don't really support scopes
        public IDisposable BeginScope<TState>(TState state) => SingletonDisposable.Instance;

        // Note: log level filtering is handled by other logging infrastructure, so we don't do any of it here.
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

        // Used for scope handling.
        private class SingletonDisposable : IDisposable
        {
            internal static readonly SingletonDisposable Instance = new SingletonDisposable();
            private SingletonDisposable() { }
            public void Dispose() { }
        }
    }
}
