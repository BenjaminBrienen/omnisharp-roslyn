using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OmniSharp.Eventing;
using OmniSharp.FileWatching;
using OmniSharp.Models.Diagnostics;
using OmniSharp.Models.FilesChanged;
using OmniSharp.Options;
using OmniSharp.Services;
using TestUtility;
using Xunit;
using Xunit.Abstractions;
using QuickFixResponse = OmniSharp.Models.v1.QuickFixResponse;
using static OmniSharp.MSBuild.Tests.ProjectLoadListenerTests;

namespace OmniSharp.MSBuild.Tests;
public class ProjectWithAnalyzersTests : AbstractMSBuildTestFixture
{
    public ProjectWithAnalyzersTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public async Task WhenProjectIsRestoredThenReanalyzeProjectAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzers").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true));
        await host.RestoreProject(testProject).ConfigureAwait(true);
        await Task.Delay(2000).ConfigureAwait(true);
        QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "Program.cs")).ConfigureAwait(true);
        Assert.NotEmpty(diagnostics.QuickFixes);
        Assert.Contains(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "IDE0060"); // Unused args.
    }

    [Fact]
    public async Task WhenProjectHasAnalyzersItDoesntLockAnalyzerDllsAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzers").ConfigureAwait(true);
        // TODO: Restore when host is running doesn't reload new analyzer references yet, move this
        // after host start after that is fixed.
        await RestoreProjectAsync(testProject).ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true));
        var analyzerReferences = host.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.ToList();
        Assert.NotEmpty(analyzerReferences);
        // This should not throw when analyzers are shadow copied.
        Directory.Delete(Path.Combine(testProject.Directory, "./nugets"), true);
    }

    [Fact]
    public async Task WhenProjectIsLoadedThenItContainsCustomRulesetsFromCsprojAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzers").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true));
        Project project = host.Workspace.CurrentSolution.Projects.Single();
        Assert.Contains(project.CompilationOptions.SpecificDiagnosticOptions, x => x.Key == "CA1021" && x.Value == ReportDiagnostic.Warn);
    }

    [Fact]
    public async Task WhenProjectIsLoadedThenItContainsAnalyzerConfigurationFromEditorConfigAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzersAndEditorConfig").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true, editorConfigEnabled: true));
        QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "Program.cs")).ConfigureAwait(true);
        Assert.NotEmpty(diagnostics.QuickFixes);
        DiagnosticLocation quickFix = diagnostics.QuickFixes.OfType<DiagnosticLocation>().Single(x => x.Id == "IDE0005");
        Assert.Equal("Error", quickFix.LogLevel);
    }

    [Fact]
    public async Task WhenProjectEditorConfigIsChangedThenAnalyzerConfigurationUpdatesAsync()
    {
        var emitter = new ProjectLoadTestEventEmitter();

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzersAndEditorConfig").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(
        testProject.Directory,
        emitter.AsExportDescriptionProvider(LoggerFactory),
        TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true, editorConfigEnabled: true));
        Project initialProject = host.Workspace.CurrentSolution.Projects.Single();
        AnalyzerConfigDocument analyzerConfigDocument = initialProject.AnalyzerConfigDocuments.Where(document => document.Name.Equals(".editorconfig", StringComparison.Ordinal)).Single();
        File.WriteAllText(analyzerConfigDocument.FilePath, @"
root = true

[*.cs]
# IDE0005: Unnecessary using
dotnet_diagnostic.IDE0005.severity = none
");

        await NotifyFileChangedAsync(host, analyzerConfigDocument.FilePath).ConfigureAwait(true);
        emitter.WaitForProjectUpdate();
        QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "Program.cs")).ConfigureAwait(true);
        Assert.NotEmpty(diagnostics.QuickFixes);
        Assert.DoesNotContain(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "IDE0005");
    }

    [Fact]
    public async Task WhenProjectChangesAnalyzerConfigurationIsPreservedAsync()
    {
        var emitter = new ProjectLoadTestEventEmitter();

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzersAndEditorConfig").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(
            testProject.Directory,
            emitter.AsExportDescriptionProvider(LoggerFactory),
            TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true, editorConfigEnabled: true));
        Project initialProject = host.Workspace.CurrentSolution.Projects.Single();
        QuickFixResponse firstDiagnosticsSet = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "Program.cs")).ConfigureAwait(true);
        Assert.NotEmpty(firstDiagnosticsSet.QuickFixes);
        Assert.Contains(firstDiagnosticsSet.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "IDE0005" && x.LogLevel == "Error");

        // report reloading of a project
        await NotifyFileChangedAsync(host, initialProject.FilePath).ConfigureAwait(true);
        emitter.WaitForProjectUpdate();
        QuickFixResponse secondDiagnosticsSet = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "Program.cs")).ConfigureAwait(true);
        Assert.NotEmpty(secondDiagnosticsSet.QuickFixes);
        Assert.Contains(secondDiagnosticsSet.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "IDE0005" && x.LogLevel == "Error");
    }

    [Fact]
    public async Task WhenProjectIsLoadedThenItContainsAnalyzerConfigurationFromParentFolderAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithParentEditorConfig").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true, editorConfigEnabled: true));
        {
            Project project = host.Workspace.CurrentSolution.Projects.Single();
            string projectFolderPath = Path.GetDirectoryName(project.FilePath);
            string projectParentFolderPath = Path.GetDirectoryName(projectFolderPath);

            AnalyzerConfigDocument analyzerConfigDocument = project.AnalyzerConfigDocuments.Where(document => document.Name.Equals(".editorconfig", StringComparison.Ordinal)).Single();
            string editorConfigFolderPath = Path.GetDirectoryName(analyzerConfigDocument.FilePath);

            Assert.Equal(projectParentFolderPath, editorConfigFolderPath);
        }
    }

    [Fact]
    public async Task WhenProjectIsLoadedThenItContainsAnalyzerConfigurationFromParentEditorConfigAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithParentEditorConfig").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true, editorConfigEnabled: true));
        {
            Project project = host.Workspace.CurrentSolution.Projects.Single();
            string projectFolderPath = Path.GetDirectoryName(project.FilePath);

            QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(projectFolderPath, "Program.cs")).ConfigureAwait(true);

            Assert.NotEmpty(diagnostics.QuickFixes);

            DiagnosticLocation quickFix = diagnostics.QuickFixes.OfType<DiagnosticLocation>().Single(x => x.Id == "IDE0005");
            Assert.Equal("Error", quickFix.LogLevel);
        }
    }

    [Fact]
    public async Task WhenProjectParentEditorConfigIsChangedThenAnalyzerConfigurationUpdatesAsync()
    {
        var emitter = new ProjectLoadTestEventEmitter();

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithParentEditorConfig").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(
            testProject.Directory,
            emitter.AsExportDescriptionProvider(LoggerFactory),
            TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true, editorConfigEnabled: true));
        {
            Project initialProject = host.Workspace.CurrentSolution.Projects.Single();
            AnalyzerConfigDocument analyzerConfigDocument = initialProject.AnalyzerConfigDocuments.Where(document => document.Name.Equals(".editorconfig", StringComparison.Ordinal)).Single();

            File.WriteAllText(analyzerConfigDocument.FilePath, @"
root = true

[*.cs]
# IDE0005: Unnecessary using
dotnet_diagnostic.IDE0005.severity = none
");

            await NotifyFileChangedAsync(host, analyzerConfigDocument.FilePath).ConfigureAwait(true);

            emitter.WaitForProjectUpdate();

            Project project = host.Workspace.CurrentSolution.Projects.Single();
            string projectFolderPath = Path.GetDirectoryName(project.FilePath);
            QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(projectFolderPath, "Program.cs")).ConfigureAwait(true);

            Assert.NotEmpty(diagnostics.QuickFixes);
            Assert.DoesNotContain(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "IDE0005");
        }
    }

    [Theory]
    [InlineData("ProjectWithDisabledAnalyzers")]
    [InlineData("ProjectWithDisabledAnalyzers2")]
    public async Task WhenProjectWithRunAnalyzersDisabledIsLoadedThenAnalyzersAreIgnoredAsync(string projectName)
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync(projectName).ConfigureAwait(true);
        await RestoreProjectAsync(testProject).ConfigureAwait(true);

        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true));
        var analyzerReferences = host.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.ToList();

        Assert.Empty(analyzerReferences);
    }

    [Fact]
    public async Task WhenProjectRulesetFileIsChangedThenUpdateRulesAccordinglyAsync()
    {
        var emitter = new ProjectLoadTestEventEmitter();

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzers").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, emitter.AsExportDescriptionProvider(LoggerFactory));
        string csprojFile = ModifyXmlFileInPlace(Path.Combine(testProject.Directory, "ProjectWithAnalyzers.csproj"),
            csprojFileXml => csprojFileXml.Descendants("CodeAnalysisRuleSet").Single().Value = "witherrorlevel.ruleset");

        await NotifyFileChangedAsync(host, csprojFile).ConfigureAwait(true);

        emitter.WaitForProjectUpdate();

        Project project = host.Workspace.CurrentSolution.Projects.Single();
        Assert.Contains(project.CompilationOptions.SpecificDiagnosticOptions, x => x.Key == "CA1021" && x.Value == ReportDiagnostic.Error);
    }

    [Fact]
    public async Task WhenProjectRulesetFileRuleIsUpdatedThenUpdateRulesAccordinglyAsync()
    {
        var emitter = new ProjectLoadTestEventEmitter();

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzers").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, emitter.AsExportDescriptionProvider(LoggerFactory));
        string ruleFile = ModifyXmlFileInPlace(Path.Combine(testProject.Directory, "default.ruleset"),
        ruleXml => ruleXml.Descendants("Rule").Single().Attribute("Action").Value = "Error");
        await NotifyFileChangedAsync(host, ruleFile).ConfigureAwait(true);
        emitter.WaitForProjectUpdate();
        Project project = host.Workspace.CurrentSolution.Projects.Single();
        Assert.Contains(project.CompilationOptions.SpecificDiagnosticOptions, x => x.Key == "CA1021" && x.Value == ReportDiagnostic.Error);
    }

    // Unstable with MSBuild 16.3 on *nix
    [ConditionalFact(typeof(WindowsOnly))]
    public async Task WhenNewAnalyzerReferenceIsAddedThenAutomaticallyUseItWithoutRestartAsync()
    {
        var emitter = new ProjectLoadTestEventEmitter();

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectWithAnalyzers").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, emitter.AsExportDescriptionProvider(LoggerFactory), configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true));
        string csprojFile = ModifyXmlFileInPlace(Path.Combine(testProject.Directory, "ProjectWithAnalyzers.csproj"),
            csprojFileXml =>
            {
                XElement referencesGroup = csprojFileXml.Descendants("ItemGroup").FirstOrDefault();
                referencesGroup.Add(new XElement("PackageReference", new XAttribute("Include", "Roslynator.Analyzers"), new XAttribute("Version", "4.1.0"), new XAttribute("PrivateAssets", "all"), new XAttribute("IncludeAssets", "runtime; build; native; contentfiles; analyzers")));
            });
        await NotifyFileChangedAsync(host, csprojFile).ConfigureAwait(true);
        emitter.WaitForProjectUpdate();
        await host.RestoreProject(testProject).ConfigureAwait(true);
        // Todo: This can be removed and replaced with wait for event (project analyzed eg.) once they are available.
        await Task.Delay(5000).ConfigureAwait(true);
        QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "Program.cs")).ConfigureAwait(true);
        Assert.Contains(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "RCS1102"); // Analysis result from roslynator.
    }

    [Fact]
    public async Task WhenProjectIsLoadedThenItRespectsDiagnosticSuppressorsAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("TwoProjectsWithAnalyzerSuppressor").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateMSBuildTestHost(testProject.Directory, configurationData: TestHelpers.GetConfigurationDataWithAnalyzerConfig(roslynAnalyzersEnabled: true));
        Project project = host.Workspace.CurrentSolution.Projects.First(p => p.Name == "App");

        // by default Stylecop reported diagnostics should be:
        //  - The file header is missing or not located at the top of the file. [App] SA1633
        //  - Elements should be documented [App] SA1600
        //  - Element 'Program' should declare an access modifier [App] SA1400
        //  - Element 'Main' should declare an access modifier [App] SA1400
        // However, SA1200 should be suppressed

        QuickFixResponse diagnostics = await host.RequestCodeCheckAsync(Path.Combine(testProject.Directory, "App", "Program.cs")).ConfigureAwait(true);
        Assert.NotEmpty(diagnostics.QuickFixes);
        Assert.Contains(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "SA1633" && x.LogLevel == "Warning");
        Assert.Contains(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "SA1600" && x.LogLevel == "Warning");
        Assert.Contains(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "SA1400" && x.LogLevel == "Warning");
        Assert.DoesNotContain(diagnostics.QuickFixes.OfType<DiagnosticLocation>(), x => x.Id == "SA1200");
    }

    private static string ModifyXmlFileInPlace(string file, Action<XDocument> docUpdateAction)
    {
        var xmlFile = XDocument.Load(file);
        docUpdateAction(xmlFile);
        xmlFile.Save(file);
        return file;
    }

    private static async Task NotifyFileChangedAsync(OmniSharpTestHost host, string file)
    {
        await host.GetFilesChangedService().Handle(new[] {
                new FilesChangedRequest() {
                FileName = file,
                ChangeType = FileChangeType.Change
                }
            }).ConfigureAwait(true);
    }

    private static async Task RestoreProjectAsync(ITestProject testProject)
    {
        DotNetCliOptions options = new() { LocationPaths = new[] { Path.Combine(TestAssets.Instance.RootFolder, DotNetCliVersion.Current.GetFolderName()) } };
        var y = new LoggerFactory();
        var x = new DotNetCliService(y, NullEventEmitter.Instance, Microsoft.Extensions.Options.Options.Create(options), new OmniSharpEnvironment(testProject.Directory));
        await x.RestoreAsync(testProject.Directory).ConfigureAwait(true);
        x.Dispose();
        y.Dispose();
    }
}
