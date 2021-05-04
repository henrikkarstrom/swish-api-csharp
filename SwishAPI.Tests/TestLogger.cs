// -------------------------------------------------------------------------------------------------
// Copyright (c) Julius Biljettservice AB. All rights reserved.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SwishAPI.Tests
{
    internal sealed class TestLogger<T> : ILogger<T>, IDisposable
    {
        private object _name;
        private ITestOutputHelper testOutputHelper;

        public TestLogger(ITestOutputHelper testOutputHelper)
        {
            _name = typeof(T).Name;
            this.testOutputHelper = testOutputHelper;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append(_name);
            logBuilder.Append("[");
            logBuilder.Append(eventId);
            logBuilder.AppendLine("]");

            testOutputHelper.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

            testOutputHelper.WriteLine($"     {_name} - {formatter(state, exception)}");
        }

        public void Dispose()
        {
        }
    }
}