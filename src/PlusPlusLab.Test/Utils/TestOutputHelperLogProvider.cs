using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PlusPlusLab.Test;

public class TestOutputHelperLogProvider(Func<ITestOutputHelper?> testOutputHelperProvider) : ILoggerProvider
{
    public void Dispose() { }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputHelperLogger(testOutputHelperProvider);
    }

    private class TestOutputHelperLogger(Func<ITestOutputHelper?> testOutputHelperProvider) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            testOutputHelperProvider()?.WriteLine(formatter(state, exception));
        }
        
        public bool IsEnabled(LogLevel logLevel) => true;
        
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
