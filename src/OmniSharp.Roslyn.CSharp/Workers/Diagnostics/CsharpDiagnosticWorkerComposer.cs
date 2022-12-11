using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Options;
using OmniSharp.Roslyn.CSharp.Services.Diagnostics;
using OmniSharp.Services;

namespace OmniSharp.Roslyn.CSharp.Workers.Diagnostics;

// Theres several implementation of worker currently based on configuration.
// This will handle switching between them.
[Export(typeof(ICsDiagnosticWorker)), Shared]
public sealed class CsharpDiagnosticWorkerComposer : ICsDiagnosticWorker, IDisposable
{
    private readonly OmniSharpWorkspace _workspace;
    private readonly IEnumerable<ICodeActionProvider> _providers;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DiagnosticEventForwarder _forwarder;
    private readonly IOptionsMonitor<OmniSharpOptions> _options;
    private ICsDiagnosticWorker _implementation;
    private readonly IDisposable _onChange;

    [ImportingConstructor]
    public CsharpDiagnosticWorkerComposer(
        OmniSharpWorkspace workspace,
        [ImportMany] IEnumerable<ICodeActionProvider> providers,
        ILoggerFactory loggerFactory,
        DiagnosticEventForwarder forwarder,
        IOptionsMonitor<OmniSharpOptions> options)
    {
        _workspace = workspace;
        _providers = providers;
        _loggerFactory = loggerFactory;
        _forwarder = forwarder;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onChange = options.OnChange(UpdateImplementation);
        UpdateImplementation(options.CurrentValue);
    }

    [MemberNotNull(nameof(_implementation))]
    private void UpdateImplementation(OmniSharpOptions options)
    {
        bool firstRun = _implementation is null;
        if (options.RoslynExtensionsOptions.EnableAnalyzersSupport && (firstRun || _implementation is CSharpDiagnosticWorker))
        {
            ICsDiagnosticWorker? old = Interlocked.Exchange(ref _implementation, new CSharpDiagnosticWorkerWithAnalyzers(_workspace, _providers, _loggerFactory, _forwarder, options));
            if (old is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        else if (!options.RoslynExtensionsOptions.EnableAnalyzersSupport && (firstRun || _implementation is CSharpDiagnosticWorkerWithAnalyzers))
        {
            ICsDiagnosticWorker? old = Interlocked.Exchange(ref _implementation, new CSharpDiagnosticWorker(_workspace, _forwarder, _loggerFactory, _options.CurrentValue));
            if (old is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (!firstRun)
            {
                _implementation.QueueDocumentsForDiagnostics();
            }
        }
        if (_implementation is null)
        {
            throw new InvalidOperationException($"Failed to set {nameof(_implementation)}");
        }
    }

    public Task<ImmutableArray<DocumentDiagnostics>> GetAllDiagnosticsAsync() => _implementation.GetAllDiagnosticsAsync();

    public Task<ImmutableArray<DocumentDiagnostics>> GetDiagnostics(ImmutableArray<string> documentPaths) => _implementation.GetDiagnostics(documentPaths);

    public ImmutableArray<DocumentId> QueueDocumentsForDiagnostics() => _implementation.QueueDocumentsForDiagnostics();

    public ImmutableArray<DocumentId> QueueDocumentsForDiagnostics(ImmutableArray<ProjectId> projectIds) => _implementation.QueueDocumentsForDiagnostics(projectIds);

    public void Dispose()
    {
        if (_implementation is IDisposable disposable)
            disposable.Dispose();
        _onChange.Dispose();
    }

    public Task<IEnumerable<Diagnostic>> AnalyzeDocumentAsync(Document document, CancellationToken cancellationToken) => _implementation.AnalyzeDocumentAsync(document, cancellationToken);

    public Task<IEnumerable<Diagnostic>> AnalyzeProjectsAsync(Project project, CancellationToken cancellationToken) => _implementation.AnalyzeProjectsAsync(project, cancellationToken);
}
