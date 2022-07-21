using Serilog.Core;
using Serilog.Events;
using Yggdrasil.Api;

namespace Merlin;

internal sealed class LoggingAggregator : ILogEventSink
{
    private const int LogsSize = 1000;

    public static readonly LoggingAggregator Instance = new();
    
    private LoggingAggregator() { }

    public IFormatProvider? FormatProvider { get; set; }

    private int _version;
    private int _gatheredVersion;

    private readonly List<string> _logs = new();
    private string[] _cache = Array.Empty<string>();

    public bool NeedUpdate
    {
        get
        {
            lock (_logs)
            {
                var needed = _version != _gatheredVersion;
                _gatheredVersion = _version;

                if (needed)
                {
                    _cache = _logs.ToArray();
                }

                return needed;
            }
        }
    }

    public string[] Gather()
    {
        lock (_logs)
        {
            return _cache;
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
            _logs.Add(@string);
            _version++;

            while (_logs.Count > LogsSize)
            {
                _logs.RemoveAt(0);
            }
        }
    }
}