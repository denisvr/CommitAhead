using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Infrastructure.Tests.AI;

/// <summary>Captures every formatted log message across all categories/levels — used to prove a value is genuinely redacted from logging output, not merely that some log level happens to be disabled.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentBag<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly ConcurrentBag<string> _messages;

        public CapturingLogger(ConcurrentBag<string> messages)
        {
            _messages = messages;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }
    }
}
