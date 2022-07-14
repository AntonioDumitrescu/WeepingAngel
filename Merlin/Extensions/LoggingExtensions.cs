using Serilog;
using Serilog.Configuration;

namespace Merlin.Extensions;

internal static class LoggingExtensions
{
    public static LoggerConfiguration Aggregator(this LoggerSinkConfiguration configuration, IFormatProvider? provider = null)
    {
        LoggingAggregator.Instance.FormatProvider = provider;
        return configuration.Sink(LoggingAggregator.Instance);
    }
}