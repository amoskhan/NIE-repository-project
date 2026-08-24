using Microsoft.Extensions.Logging;

namespace Auth.Tests.TestDoubles;

/// <summary>
/// A single captured log call. <see cref="AllText"/> flattens everything that would reach a log
/// sink (message, structured state values and exception detail) so audit tests can assert that no
/// credential or session token ever leaves the process through the logging pipeline.
/// </summary>
internal sealed record LogEntry(
    LogLevel Level,
    string Message,
    Exception? Exception,
    IReadOnlyList<string> StateValues)
{
    public string AllText =>
        string.Join('\n', new[] { Message, Exception?.ToString() ?? string.Empty }.Concat(StateValues));
}

/// <summary>
/// <see cref="ILogger{TCategoryName}"/> recorder. NSubstitute cannot usefully intercept the generic
/// <c>Log&lt;TState&gt;</c> member because the state type is an internal framework struct, so the
/// interface is implemented directly.
/// </summary>
internal sealed class RecordingLogger<TCategoryName> : ILogger<TCategoryName>
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public string AllLoggedText => string.Join('\n', _entries.Select(entry => entry.AllText));

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var values = state as IReadOnlyList<KeyValuePair<string, object?>>;
        _entries.Add(new LogEntry(
            logLevel,
            formatter(state, exception),
            exception,
            values is null ? [] : [.. values.Select(pair => pair.Value?.ToString() ?? string.Empty)]));
    }
}
