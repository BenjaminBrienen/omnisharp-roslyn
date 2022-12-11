using System;
using System.ComponentModel;
using McMaster.Extensions.CommandLineUtils;

namespace OmniSharp.Internal;

public static class CommandOptionExtensions
{
    public static T GetValueOrDefault<T>(this CommandOption opt, T defaultValue)
    {
        return opt is null ? throw new ArgumentNullException(nameof(opt))
            : opt.HasValue()
            && opt.Value() is object o
            && TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(o) is T t
                ? t : defaultValue;
    }

    public static bool GetValueOrDefault(this CommandOption opt, bool defaultValue)
    {
        return opt is null
            ? throw new ArgumentNullException(nameof(opt))
            : opt.Value()?.Equals("on", StringComparison.OrdinalIgnoreCase) == true || defaultValue;
    }
}
