using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Tests;

public sealed record TestLog(string Message, IReadOnlyDictionary<string, object?> Fields);

public sealed class TestLogProvider : ILoggerProvider
{
    public ConcurrentQueue<TestLog> Entries { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Entries);
    public void Dispose() { }

    private sealed class CaptureLogger(ConcurrentQueue<TestLog> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var fields = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(entry => entry.Key, entry => entry.Value)
                : new Dictionary<string, object?>();
            entries.Enqueue(new TestLog(formatter(state, exception), fields));
        }
    }
}
