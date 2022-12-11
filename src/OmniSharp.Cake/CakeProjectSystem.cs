using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cake.Scripting.Abstractions.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Cake.Services;
using OmniSharp.FileSystem;
using OmniSharp.FileWatching;
using OmniSharp.Helpers;
using OmniSharp.Mef;
using OmniSharp.Models.V1.WorkspaceInformation;
using OmniSharp.Roslyn;
using OmniSharp.Roslyn.EditorConfig;
using OmniSharp.Roslyn.Utilities;
using OmniSharp.Services;

namespace OmniSharp.Cake;

[ExportProjectSystem(ProjectSystemNames.CakeProjectSystem), Shared]
public class CakeProjectSystem : IProjectSystem
{
    private readonly OmniSharpWorkspace _workspace;
    private readonly MetadataFileReferenceCache _metadataReferenceCache;
    private readonly IOmniSharpEnvironment _environment;
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly ICakeScriptService _scriptService;
    private readonly IFileSystemWatcher _fileSystemWatcher;
    private readonly FileSystemHelper _fileSystemHelper;
    private readonly ILogger<CakeProjectSystem> _logger;
    private readonly ConcurrentDictionary<string, ProjectInfo> _projects;
    private readonly Lazy<CSharpCompilationOptions> _compilationOptions;
    private CakeOptions? _options;
    public string Key { get; } = "Cake";
    public string Language { get; } = Constants.LanguageNames.Cake;
    public IEnumerable<string> Extensions { get; } = new[] { ".cake" };
    public bool EnabledByDefault { get; } = true;
    public bool Initialized { get; private set; }

    [ImportingConstructor]
    public CakeProjectSystem(
        OmniSharpWorkspace workspace,
        MetadataFileReferenceCache metadataReferenceCache,
        IOmniSharpEnvironment environment,
        IAssemblyLoader assemblyLoader,
        ICakeScriptService scriptService,
        IFileSystemWatcher fileSystemWatcher,
        FileSystemHelper fileSystemHelper,
        ILoggerFactory loggerFactory)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _metadataReferenceCache = metadataReferenceCache ?? throw new ArgumentNullException(nameof(metadataReferenceCache));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));
        _scriptService = scriptService ?? throw new ArgumentNullException(nameof(scriptService));
        _fileSystemWatcher = fileSystemWatcher ?? throw new ArgumentNullException(nameof(fileSystemWatcher));
        _fileSystemHelper = fileSystemHelper;
        _logger = loggerFactory?.CreateLogger<CakeProjectSystem>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        _projects = new ConcurrentDictionary<string, ProjectInfo>();
        _compilationOptions = new Lazy<CSharpCompilationOptions>(CreateCompilationOptions);
    }

    public void Initalize(IConfiguration configuration)
    {
        if (Initialized)
            return;

        _options = new CakeOptions();
        configuration.Bind(_options);

            _logger.LogInformation($"Detecting Cake files in '{_environment.TargetDirectory}'.");

        // Nothing to do if there are no Cake files
        string[] allCakeFiles = _fileSystemHelper.GetFiles("**/*.cake").ToArray();
        if (allCakeFiles.Length == 0)
        {
                _logger.LogInformation("Did not find any Cake files");
            return;
        }

            _logger.LogInformation($"Found {allCakeFiles.Length} Cake files.");

        // Try intialize Cake scripting service
        if (!_scriptService.Initialize(_options))
        {
                _logger.LogWarning("Could not initialize Cake script service. Aborting.");
            return;
        }

        foreach (string? cakeFilePath in allCakeFiles)
        {
            AddCakeFile(cakeFilePath);
        }

        // Hook up Cake script events
        _scriptService.ReferencesChanged += ScriptReferencesChanged;
        _scriptService.UsingsChanged += ScriptUsingsChanged;

        // Watch .cake files
        _fileSystemWatcher.Watch(".cake", OnCakeFileChanged);

        Initialized = true;
    }

    private void AddCakeFile(string cakeFilePath)
    {
        try
        {
            CakeScript cakeScript = _scriptService.Generate(new FileChange
            {
                FileName = cakeFilePath,
                FromDisk = true
            });

            ProjectInfo project = GetProject(cakeScript, cakeFilePath);

            // Add Cake project to workspace
            _workspace.AddProject(project);
            var documentId = DocumentId.CreateNewId(project.Id);
            var loader = TextLoader.From(TextAndVersion.Create(SourceText.From(cakeScript.Source), VersionStamp.Create(DateTime.UtcNow)));
            var documentInfo = DocumentInfo.Create(
                documentId,
                cakeFilePath,
                filePath: cakeFilePath,
                loader: loader,
                sourceCodeKind: SourceCodeKind.Script);

            _workspace.AddDocument(documentInfo);
            _projects[cakeFilePath] = project;
                _logger.LogInformation($"Added Cake project '{cakeFilePath}' to the workspace.");
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
                _logger.LogError(ex, $"{cakeFilePath} will be ignored due to an following error");
        }
#pragma warning restore CA1031
    }

    private void RemoveCakeFile(string cakeFilePath)
    {
        if (_projects.TryRemove(cakeFilePath, out ProjectInfo? projectInfo))
        {
            _workspace.RemoveProject(projectInfo.Id);
                _logger.LogInformation($"Removed Cake project '{cakeFilePath}' from the workspace.");
        }
    }

    private void OnCakeFileChanged(string filePath, FileChangeType changeType)
    {
        if ((changeType == FileChangeType.Unspecified && !File.Exists(filePath)) || changeType == FileChangeType.Delete)
        {
            RemoveCakeFile(filePath);
        }

        if ((changeType == FileChangeType.Unspecified && File.Exists(filePath)) || changeType == FileChangeType.Create)
        {
            AddCakeFile(filePath);
        }
    }

    private void ScriptUsingsChanged(object? sender, UsingsChangedEventArgs e)
    {
        Solution solution = _workspace.CurrentSolution;

        ImmutableArray<DocumentId> documentIds = solution.GetDocumentIdsWithFilePath(e.ScriptPath);
        if (documentIds.IsEmpty)
        {
            return;
        }

        CSharpCompilationOptions compilationOptions = e.Usings is null
            ? _compilationOptions.Value
            : _compilationOptions.Value.WithUsings(e.Usings);

        foreach (DocumentId documentId in documentIds)
        {
            Document document = solution.GetDocument(documentId) ?? throw new InvalidOperationException($"Missing document {documentId.Id} in {documentId.ProjectId}");
            Project project = document.Project;

            _workspace.SetCompilationOptions(project.Id, compilationOptions);
        }
    }

    private void ScriptReferencesChanged(object? sender, ReferencesChangedEventArgs e)
    {
        Solution solution = _workspace.CurrentSolution;

        ImmutableArray<DocumentId> documentIds = solution.GetDocumentIdsWithFilePath(e.ScriptPath);
        if (documentIds.IsEmpty)
        {
            return;
        }

        foreach (DocumentId documentId in documentIds)
        {
            Document document = solution.GetDocument(documentId) ?? throw new InvalidOperationException($"Missing document {documentId.Id} in {documentId.ProjectId}");
            Project project = document.Project;

            IEnumerable<MetadataReference> metadataReferences = GetMetadataReferences(e.References);
            var referencesToRemove = new HashSet<MetadataReference>(project.MetadataReferences, MetadataReferenceEqualityComparer.Instance);
            var referencesToAdd = new HashSet<MetadataReference>(MetadataReferenceEqualityComparer.Instance);

            foreach (MetadataReference reference in metadataReferences)
            {
                if (referencesToRemove.Remove(reference))
                {
                    continue;
                }

                if (referencesToAdd.Contains(reference))
                {
                    continue;
                }

                _workspace.AddMetadataReference(project.Id, reference);
                referencesToAdd.Add(reference);
            }

            foreach (MetadataReference reference in referencesToRemove)
            {
                _workspace.RemoveMetadataReference(project.Id, reference);
            }
        }
    }

    public Task WaitForIdleAsync() => Task.CompletedTask;

    public Task<object> GetWorkspaceModelAsync(WorkspaceInformationRequest request)
    {
        var scriptContextModels = new List<CakeContextModel>();
        foreach (KeyValuePair<string, ProjectInfo> project in _projects)
        {
            scriptContextModels.Add(new CakeContextModel(project.Key));
        }
        return Task.FromResult<object>(new CakeContextModelCollection(scriptContextModels));
    }

    public Task<object?> GetProjectModelAsync(string filePath)
    {
        if (filePath is null)
            throw new ArgumentNullException(nameof(filePath));

        // Only react to .cake file paths
        if (filePath.EndsWith(".cake", StringComparison.OrdinalIgnoreCase))
        {
            Document document = _workspace.GetDocument(filePath);
            string? projectFilePath = document is not null ? document.Project.FilePath : filePath;
            ProjectInfo? projectInfo = GetProjectFileInfo(projectFilePath);
            if (projectInfo is not null)
            {
                return Task.FromResult<object?>(new CakeContextModel(filePath));
            }
            else
            {
                _logger.LogDebug($"Could not locate project for '{projectFilePath}'");
                return Task.FromResult<object>(null);
            }
        }
        return Task.FromResult<object?>(null);
    }

    private ProjectInfo? GetProjectFileInfo(string? path) => path is not null && _projects.TryGetValue(path, out ProjectInfo? projectFileInfo) ? projectFileInfo : null;

    private ProjectInfo GetProject(CakeScript cakeScript, string filePath)
    {
        string name = Path.GetFileName(filePath);

        if (!File.Exists(cakeScript.Host.AssemblyPath))
        {
            throw new FileNotFoundException($"Cake is not installed. Path {cakeScript.Host.AssemblyPath} does not exist.");
        }
        var hostObjectType = Type.GetType(cakeScript.Host.TypeName, a => _assemblyLoader.LoadFrom(cakeScript.Host.AssemblyPath, dontLockAssemblyOnDisk: true), null, false);
        if (hostObjectType is null)
        {
            throw new InvalidOperationException($"Could not get host object type: {cakeScript.Host.TypeName}.");
        }

        var projectId = ProjectId.CreateNewId(Guid.NewGuid().ToString());
        ImmutableArray<DocumentInfo> analyzerConfigDocuments = _workspace.EditorConfigEnabled
            ? EditorConfigFinder
                .GetEditorConfigPaths(filePath)
                .Select(path =>
                    DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId),
                        name: ".editorconfig",
                        loader: new FileTextLoader(path, Encoding.UTF8),
                        filePath: path))
                .ToImmutableArray()
            : ImmutableArray<DocumentInfo>.Empty;

        return ProjectInfo.Create(
            id: projectId,
            version: VersionStamp.Create(),
            name: name,
            filePath: filePath,
            assemblyName: $"{name}.dll",
            language: LanguageNames.CSharp,
            compilationOptions: cakeScript.Usings is null ? _compilationOptions.Value : _compilationOptions.Value.WithUsings(cakeScript.Usings),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Script),
            metadataReferences: GetMetadataReferences(cakeScript.References),
            // TODO: projectReferences?
            isSubmission: true,
            hostObjectType: hostObjectType)
            .WithAnalyzerConfigDocuments(analyzerConfigDocuments);
    }

    private IEnumerable<MetadataReference> GetMetadataReferences(IEnumerable<string> references)
    {
        foreach (string reference in references)
        {
            if (!File.Exists(reference))
            {
                    _logger.LogWarning($"Unable to create MetadataReference. File {reference} does not exist.");
                continue;
            }

            yield return _metadataReferenceCache.GetMetadataReference(reference);
        }
    }

    private static CSharpCompilationOptions CreateCompilationOptions()
    {
        CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                metadataReferenceResolver: new CachingScriptMetadataResolver(),
                sourceReferenceResolver: ScriptSourceResolver.Default,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default).
                WithSpecificDiagnosticOptions(CompilationOptionsHelper.GetDefaultSuppressedDiagnosticOptions());

        PropertyInfo? topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
        Type? binderFlagsType = typeof(CSharpCompilationOptions).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.CSharp.BinderFlags");

        FieldInfo? ignoreCorLibraryDuplicatedTypesMember = binderFlagsType?.GetField("IgnoreCorLibraryDuplicatedTypes", BindingFlags.Static | BindingFlags.Public);
        object? ignoreCorLibraryDuplicatedTypesValue = ignoreCorLibraryDuplicatedTypesMember?.GetValue(null);
        if (ignoreCorLibraryDuplicatedTypesValue is not null)
        {
            topLevelBinderFlagsProperty?.SetValue(compilationOptions, ignoreCorLibraryDuplicatedTypesValue);
        }

        return compilationOptions;
    }
}
