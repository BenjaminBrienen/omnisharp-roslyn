using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OmniSharp.Options;

public record RoslynExtensionsOptions
(
    bool EnableDecompilationSupport,
    bool EnableAnalyzersSupport,
    bool EnableImportCompletion,
    bool EnableAsyncCompletion,
    int DocumentAnalysisTimeoutMs,
    bool AnalyzeOpenDocumentsOnly,
    InlayHintsOptions InlayHintsOptions,
    IEnumerable<string> LocationPaths,
    int DiagnosticWorkersThreadCount
) : OmniSharpExtensionsOptions(LocationPaths);

public record OmniSharpExtensionsOptions(IEnumerable<string> LocationPaths)
{
    public IEnumerable<string> GetNormalizedLocationPaths(IOmniSharpEnvironment env)
    {
        if (env is null)
            throw new ArgumentNullException(nameof(env));
        if (LocationPaths is null || !LocationPaths.Any())
            return Enumerable.Empty<string>();

        var normalizePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string locationPath in LocationPaths)
        {
            if (Path.IsPathRooted(locationPath))
            {
                normalizePaths.Add(locationPath);
            }
            else
            {
                normalizePaths.Add(Path.Combine(env.TargetDirectory, locationPath));
            }
        }
        return normalizePaths;
    }
}
