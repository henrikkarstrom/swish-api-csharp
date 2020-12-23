using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SwishApi.IntegrationTests
{
    public class FakeLogger<T> : ILogger<T>, IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _loggerName;

        public FakeLogger(ITestOutputHelper testOutputHelper)
        {
            _loggerName = typeof(T).Name;
            _testOutputHelper = testOutputHelper;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _testOutputHelper.WriteLine($"{_loggerName} {logLevel} : {formatter(state, exception)}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}