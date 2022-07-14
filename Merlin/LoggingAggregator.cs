using Serilog.Core;
using Serilog.Events;
using Yggdrasil.Api;

namespace Merlin;

internal sealed class LoggingAggregator : ILogEventSink
{
    private const int LogsSize = 1000;

    private static readonly Lazy<LoggingAggregator> Lazy = new(() => new LoggingAggregator(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static LoggingAggregator Instance => Lazy.Value;

    private readonly DataPointList<string> _logs = new(LogsSize);

    private LoggingAggregator() { }

    public IFormatProvider? FormatProvider { get; set; }

    public bool NeedUpdate
    {
        get
        {
            lock (_logs) return _logs.NeedsUpdate;
        }
    }

    public string[] Gather()
    {
        lock (_logs)
        {
            return _logs.AsArray();
        }
    }

    public void Emit(LogEvent logEvent)
    {
        var @string = logEvent.Exception == null
            ? $"[{logEvent.Level}] {logEvent.RenderMessage(FormatProvider!)}"
            // how did we get here?
            : $"[{logEvent.Level}] {logEvent.RenderMessage(FormatProvider!)}\r\n{logEvent.Exception.ToString().Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n", "\n    ")}\r\n";

        lock (_logs)
        {
            _logs.AddPoint(@string);
        }
    }
}