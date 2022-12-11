using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Cake.Utilities;
using OmniSharp.FileWatching;
using OmniSharp.Roslyn;
using OmniSharp.Services;
using OmniSharp.Utilities;
using Xunit;

namespace OmniSharp.Cake.Tests
{
    public class LineIndexHelperFacts
    {
        private static Assembly Assembly => typeof(LineIndexHelperFacts).GetTypeInfo().Assembly;
        private static string ResourcePath => "OmniSharp.Cake.Tests.TestAssets";

        private static string SingleCakePath => PlatformHelper.IsWindows ? @"C:\Work\single.cake" : "/work/single.cake";

        private static string MultiCakePath => PlatformHelper.IsWindows ? @"C:\Work\multi.cake" : "/work/multi.cake";

        private static string GetResourceContent(string resourceName)
        {
            using Stream? stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new InvalidOperationException("Could not load manifest resource stream.");
            }
            using StreamReader reader = new(stream);
            return reader.ReadToEnd().TrimEnd('\r', '\n');
        }

        private static string GetGeneratedFileContent(string name)
        {
            string content = GetResourceContent($"{ResourcePath}.{Path.GetFileName(name)}.g.txt");
            if (PlatformHelper.IsWindows)
            {
                return content;
            }
            // Adjust paths in generated content
            return content.Replace("C:/Work/", "/work/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFileContent(string name) => GetResourceContent($"{ResourcePath}.{Path.GetFileName(name)}.txt");

        private static OmniSharpWorkspace CreateSimpleWorkspace(string fileName, string contents)
        {
            using LoggerFactory loggerFactory1 = new();
            using LoggerFactory loggerFactory2 = new();
            var workspace = new OmniSharpWorkspace(
                new HostServicesAggregator(Enumerable.Empty<IHostServicesProvider>(), loggerFactory1), loggerFactory2, new DummyFileSystemWatcher());
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                "ProjectNameVal", "AssemblyNameVal", LanguageNames.CSharp);
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), fileName,
                null, SourceCodeKind.Regular,
                TextLoader.From(TextAndVersion.Create(SourceText.From(contents), VersionStamp.Create())),
                fileName);
            workspace.AddProject(projectInfo);
            workspace.AddDocument(documentInfo);

            return workspace;
        }

        [Theory]
        [InlineData(0, 8209)]
        [InlineData(7, 8216)]
        public async Task TranslateToGeneratedShouldTranslateIndexInSingleFileAsync(int index, int expected)
        {
            string fileName = SingleCakePath;
            using OmniSharpWorkspace workspace = CreateSimpleWorkspace(fileName, GetGeneratedFileContent(fileName));
            int actual = await LineIndexHelper.TranslateToGenerated(fileName, index, workspace).ConfigureAwait(true);
            Assert.Equal(expected, actual);
            Assert.Equal(GetFileContent(fileName).Split('\n')[index],
                GetGeneratedFileContent(fileName).Split('\n')[actual]);
        }

        [Theory]
        [InlineData(8209, 0)]
        [InlineData(8216, 7)]
        public async Task TranslateFromGeneratedShouldTranslateIndexInSingleFileAsync(int index, int expected)
        {
            string fileName = SingleCakePath;
            using OmniSharpWorkspace workspace = CreateSimpleWorkspace(fileName, GetGeneratedFileContent(fileName));
            (int actualIndex, string actualFileName) = await LineIndexHelper.TranslateFromGenerated(fileName, index, workspace, true).ConfigureAwait(true);
            Assert.Equal(expected, actualIndex);
            Assert.Equal(GetGeneratedFileContent(fileName).Split('\n')[index],
                GetFileContent(actualFileName).Split('\n')[actualIndex]);
        }

        [Theory]
        [InlineData(0, 8209)]
        [InlineData(4, 8227)]
        public async Task TranslateToGeneratedShouldTranslateIndexWithMultipleFilesAsync(int index, int expected)
        {
            string fileName = MultiCakePath;
            using OmniSharpWorkspace workspace = CreateSimpleWorkspace(fileName, GetGeneratedFileContent(fileName));
            int actual = await LineIndexHelper.TranslateToGenerated(fileName, index, workspace).ConfigureAwait(true);

            Assert.Equal(expected, actual);
            Assert.Equal(GetFileContent(fileName).Split('\n')[index],
                GetGeneratedFileContent(fileName).Split('\n')[actual]);
        }

        [Theory]
        [InlineData(8209, 0)]
        [InlineData(8227, 4)]
        public async Task TranslateFromGeneratedShouldTranslateIndexWithMultipleFilesAsync(int index, int expected)
        {
            string fileName = MultiCakePath;
            using OmniSharpWorkspace workspace = CreateSimpleWorkspace(fileName, GetGeneratedFileContent(fileName));

            (int actualIndex, string actualFileName) = await LineIndexHelper.TranslateFromGenerated(fileName, index, workspace, true).ConfigureAwait(true);

            Assert.Equal(expected, actualIndex);
            Assert.Equal(GetGeneratedFileContent(fileName).Split('\n')[index],
                GetFileContent(actualFileName).Split('\n')[actualIndex]);
        }

        [Fact]
        public async Task TranslateFromGeneratedShouldTranslateToNegativeIfOutsideBoundsAsync()
        {
            const int index = 8207;
            const int expected = -1;
            string fileName = SingleCakePath;
            using OmniSharpWorkspace workspace = CreateSimpleWorkspace(fileName, GetGeneratedFileContent(fileName));

            (int actualIndex, _) = await LineIndexHelper.TranslateFromGenerated(fileName, index, workspace, true).ConfigureAwait(true);

            Assert.Equal(expected, actualIndex);
        }

        private class DummyFileSystemWatcher : IFileSystemWatcher
        {
            public void Watch(string pathOrExtension, FileSystemNotificationCallback callback) { }

            public void WatchDirectories(FileSystemNotificationCallback callback) { }
        }
    }
}
