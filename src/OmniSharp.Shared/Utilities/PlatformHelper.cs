using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace OmniSharp.Utilities;

public static class PlatformHelper
{
    private static IEnumerable<string>? s_searchPaths;
    private static string? s_monoRuntimePath;
    private static string? s_monoLibDirPath;

    public static bool IsMono => Type.GetType("Mono.Runtime") is not null;
    public static bool IsWindows => Path.DirectorySeparatorChar == '\\';

    public static IEnumerable<string> GetSearchPaths()
    {
        if (s_searchPaths is null)
        {
            string? path = Environment.GetEnvironmentVariable("PATH");
            if (path is null)
            {
                return Array.Empty<string>();
            }

            s_searchPaths = path
                .Split(Path.PathSeparator)
                .Select(p => p.Trim('"'));
        }

        return s_searchPaths;
    }

    /// <summary>
    /// Returns the conanicalized absolute path from a given path, expanding symbolic links and resolving
    /// references to /./, /../ and extra '/' path characters.
    /// </summary>
    private static string? RealPath(string path)
    {
        if (IsWindows)
        {
            throw new PlatformNotSupportedException($"{nameof(RealPath)} can only be called on Unix.");
        }

        IntPtr ptr = NativeMethods.Unix_realpath(path, IntPtr.Zero);
        string? result = Marshal.PtrToStringAnsi(ptr); // uses UTF8 on Unix
        NativeMethods.Unix_free(ptr);

        return result;
    }

    public static Version GetMonoVersion()
    {
        string output = ProcessHelper.RunAndCaptureOutput("mono", "--version");

        // The mono --version text contains several lines. We'll just walk through the first line,
        // word by word, until we find a word that parses as a version number. Normally, this should
        // be the *fifth* word. E.g. "Mono JIT compiler version 4.8.0"

        string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        string[] words = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            if (Version.TryParse(word, out Version? version))
            {
                return version;
            }
        }
        throw new InvalidOperationException($"Couldn't parse version from output of 'mono --version':\n{output}");
    }

    public static string? GetMonoRuntimePath()
    {
        if (IsWindows)
        {
            return null;
        }

        if (s_monoRuntimePath is null)
        {
            string? monoPath = GetSearchPaths()
                .Select(p => Path.Combine(p, "mono"))
                .FirstOrDefault(File.Exists);

            if (monoPath is null)
            {
                return null;
            }

            s_monoRuntimePath = RealPath(monoPath);
        }

        return s_monoRuntimePath;
    }

    public static string? GetMonoLibDirPath()
    {
        if (IsWindows)
        {
            return null;
        }

        const string DefaultMonoLibPath = "/usr/lib/mono";
        if (Directory.Exists(DefaultMonoLibPath))
        {
            return DefaultMonoLibPath;
        }

        // The normal Unix path doesn't exist, so we'll fallback to finding Mono using the
        // runtime location. This is the likely situation on macOS.

        if (s_monoLibDirPath is null)
        {
            string? monoRuntimePath = GetMonoRuntimePath();
            if (monoRuntimePath is null)
            {
                return null;
            }
            // GetDirectoryName: null if path denotes a root directory or is null
            string monoDirPath = Path.GetDirectoryName(monoRuntimePath) ?? throw new InvalidOperationException($"{nameof(monoRuntimePath)} cannot be {monoRuntimePath}");
            string monoLibDirPath = Path.Combine(monoDirPath, "..", "lib", "mono");
            monoLibDirPath = Path.GetFullPath(monoLibDirPath);

            s_monoLibDirPath = Directory.Exists(monoLibDirPath)
                ? monoLibDirPath
                : null;
        }

        return s_monoLibDirPath;
    }

    public static string? GetMonoMSBuildDirPath()
    {
        if (IsWindows)
        {
            return null;
        }

        string? monoLibDirPath = GetMonoLibDirPath();
        if (monoLibDirPath is null)
        {
            return null;
        }

        string monoMSBuildDirPath = Path.Combine(monoLibDirPath, "msbuild");
        monoMSBuildDirPath = Path.GetFullPath(monoMSBuildDirPath);

        return Directory.Exists(monoMSBuildDirPath)
            ? monoMSBuildDirPath
            : null;
    }
    private static class NativeMethods
    {
        // http://man7.org/linux/man-pages/man3/realpath.3.html
        [DllImport("libc", EntryPoint = "realpath", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        public static extern IntPtr Unix_realpath(string path, IntPtr buffer);

        // http://man7.org/linux/man-pages/man3/free.3.html
        [DllImport("libc", EntryPoint = "free", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        public static extern void Unix_free(IntPtr ptr);
    }
}
