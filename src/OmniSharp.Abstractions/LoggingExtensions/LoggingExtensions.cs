using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.LoggingExtensions;

namespace OmniSharp.LoggingExtensions;

[InterpolatedStringHandler]
public record struct LoggerInterpolatedStringHandler : IEquatable<LoggerInterpolatedStringHandler>
{
    private readonly StringBuilder? _builder;
    public LoggerInterpolatedStringHandler(int literalLength, ILogger logger, LogLevel level)
    {
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        _builder = logger.IsEnabled(level) ? (new(literalLength)) : null;
    }

    public void AppendLiteral(string literal)
    {
        Debug.Assert(_builder is not null);
        _builder!.Append(literal);
    }

    public void AppendFormatted<T>(T t)
    {
        Debug.Assert(_builder is not null);
        _builder!.Append(t?.ToString());
    }

    public void AppendFormatted<T>(T t, int alignment, string format)
    {
        Debug.Assert(_builder is not null);
        _builder!.Append(string.Format(CultureInfo.InvariantCulture, $"{{0,{alignment}:{format}}}", t));
    }

    public override string ToString() => _builder?.ToString() ?? string.Empty;
}

#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class InterpolatedStringHandlerAttribute : Attribute
{
}
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    public InterpolatedStringHandlerArgumentAttribute(string argument) => Arguments = new string[] { argument };

    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => Arguments = arguments;

    public string[] Arguments { get; }
}
}
#endif
