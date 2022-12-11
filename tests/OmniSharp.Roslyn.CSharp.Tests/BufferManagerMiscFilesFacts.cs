using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Models;
using OmniSharp.Models.CodeCheck;
using OmniSharp.Models.Diagnostics;
using OmniSharp.Models.FilesChanged;
using OmniSharp.Models.FindImplementations;
using OmniSharp.Models.FindUsages;
using OmniSharp.Models.SignatureHelp;
using OmniSharp.Models.TypeLookup;
using OmniSharp.Models.v1;
using OmniSharp.Roslyn.CSharp.Services.Files;
using OmniSharp.Roslyn.CSharp.Services.Types;
using TestUtility;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using OmniSharp.Models.v1.FindSymbols;
using OmniSharp.Models.V1.SignatureHelp;
using OmniSharp.Models.V1.FixUsings;

namespace OmniSharp.Tests;
public class BufferManagerMiscFilesFacts : AbstractTestFixture
{
    public BufferManagerMiscFilesFacts(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsOnlySyntacticDiagnosticsAsync()
    {
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        var testfile = new TestFile("a.cs", "class C { b a = new b(); int n  }");
        OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        CodeCheckRequest request = new() { FileName = filePath };
        QuickFixResponse actual = await host.GetResponse<CodeCheckRequest, QuickFixResponse>(OmniSharpEndpoints.CodeCheck, request).ConfigureAwait(true);
        Assert.Single(actual.QuickFixes);
        Assert.Equal("; expected", actual.QuickFixes.First().Text);
        Assert.Equal("CS1002", actual.QuickFixes.OfType<DiagnosticLocation>().First().Id);
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsSignatureHelpAsync()
    {
        const string source =
@"class Program
{
public static void Main(){
    System.Guid.NewGuid($$);
}
}";
        var testfile = new TestFile("a.cs", source);

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        TextPoint point = testfile.Content.GetPointFromPosition();
        var request = new SignatureHelpRequest()
        {
            FileName = filePath,
            Line = point.Line,
            Column = point.Offset,
            Buffer = testfile.Content.Code
        };

        SignatureHelpResponse actual = await host.GetResponse<SignatureHelpRequest, SignatureHelpResponse>(OmniSharpEndpoints.SignatureHelp, request).ConfigureAwait(true);
        Assert.Single(actual.Signatures);
        Assert.Equal(0, actual.ActiveParameter);
        Assert.Equal(0, actual.ActiveSignature);
        Assert.Equal("NewGuid", actual.Signatures.ElementAt(0).Name);
        Assert.Empty(actual.Signatures.ElementAt(0).Parameters);
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsImplementationsAsync()
    {
        const string source = @"
            public class MyClass
            {
                public MyClass() { Fo$$o(); }
                public void Foo() {}
            }";

        var testfile = new TestFile("a.cs", source);

        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        TextPoint point = testfile.Content.GetPointFromPosition();
        var request = new FindImplementationsRequest()
        {
            FileName = filePath,
            Line = point.Line,
            Column = point.Offset,
            Buffer = testfile.Content.Code
        };

        QuickFixResponse actual = await host.GetResponse<FindImplementationsRequest, QuickFixResponse>(OmniSharpEndpoints.FindImplementations, request).ConfigureAwait(true);
        Assert.Single(actual.QuickFixes);
        Assert.Equal("public void Foo() {}", actual.QuickFixes.First().Text.Trim());
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsUsagesAsync()
    {
        const string source = @"
public class F$$oo
{
    public string prop { get; set; }
}
public class FooConsumer
{
    public FooConsumer()
    {
        var temp = new Foo();
        var prop = foo.prop;
    }
}";

        var testfile = new TestFile("a.cs", source);
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        TextPoint point = testfile.Content.GetPointFromPosition();
        var request = new FindUsagesRequest()
        {
            FileName = filePath,
            Line = point.Line,
            Column = point.Offset,
            Buffer = testfile.Content.Code
        };
        QuickFixResponse actual = await host.GetResponse<FindUsagesRequest, QuickFixResponse>(OmniSharpEndpoints.FindUsages, request).ConfigureAwait(true);
        Assert.Equal(2, actual.QuickFixes.Count());
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsSymbolsAsync()
    {
        const string source = @"
namespace Some.Long.Namespace
            {
                public class Foo
                {
                    private string field = 0;
                    private string AutoProperty { get; }
                    private string Property
                    {
                        get { return field; }
                        set { field = value; }
                    }
                    private string Method() {}
                    private string Method(string param) {}
                    private class Nested
                    {
                        private string NestedMethod() {}
                    }
                }
            }";

        var testfile = new TestFile("a.cs", source);
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        QuickFixResponse actual = await host.GetResponse<FindSymbolsRequest, QuickFixResponse>(OmniSharpEndpoints.FindSymbols, null).ConfigureAwait(true);
        IEnumerable<string> symbols = actual.QuickFixes.Select(q => q.Text);
        string[] expected = new[]
        {
            "Foo",
            "field",
            "AutoProperty",
            "Property",
            "Method()",
            "Method(string param)",
            "Nested",
            "NestedMethod()"
        };
        Assert.Equal(expected, symbols);
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsFixUsingsAsync()
    {
        const string code = @"
namespace nsA
{
public class classX{}
}
namespace OmniSharp
{
public class class1
{
    public method1()
    {
        var c1 = new classX();
    }
}
}";

        const string expectedCode = @"
using nsA;

namespace nsA
{
public class classX{}
}
namespace OmniSharp
{
public class class1
{
    public method1()
    {
        var c1 = new classX();
    }
}
}";

        var testfile = new TestFile("a.cs", code);
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        var request = new FixUsingsRequest() { FileName = filePath };
        FixUsingsResponse actual = await host.GetResponse<FixUsingsRequest, FixUsingsResponse>(OmniSharpEndpoints.FixUsings, request).ConfigureAwait(true);
        Assert.Equal(expectedCode.Replace("\r\n", "\n", System.StringComparison.InvariantCulture), actual.Buffer.Replace("\r\n", "\n", System.StringComparison.InvariantCulture));
    }

    [Fact]
    public async Task AddsMiscDocumentWhichSupportsTypeLookupAsync()
    {
        const string code = @"class F$$oo {}";
        var testfile = new TestFile("a.cs", code);
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        TypeLookupService service = host.GetRequestHandler<TypeLookupService>(OmniSharpEndpoints.TypeLookup);
        TextPoint point = testfile.Content.GetPointFromPosition();
        var request = new TypeLookupRequest
        {
            FileName = filePath,
            Line = point.Line,
            Column = point.Offset,
        };
        TypeLookupResponse actual = await host.GetResponse<TypeLookupRequest, TypeLookupResponse>(OmniSharpEndpoints.TypeLookup, request).ConfigureAwait(true);
        Assert.Equal("Foo", actual.Type);
    }

    [Fact]
    public async Task AddsMultipleMiscFilesToSameProjectAsync()
    {
        const string source1 =
@"class Program
{
public static void Main(){
    A a = new A(4, $$5);
}
}";

        const string source2 =
@"class A
{
A(int a, int b)
{
}
}";
        var testfile1 = new TestFile("file1.cs", source1);
        var testfile2 = new TestFile("file2.cs", source2);
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath1 = await AddTestFileAsync(host, testProject, testfile1).ConfigureAwait(true);
        string filePath2 = await AddTestFileAsync(host, testProject, testfile2).ConfigureAwait(true);
        TextPoint point = testfile1.Content.GetPointFromPosition();
        var request = new SignatureHelpRequest()
        {
            FileName = filePath1,
            Line = point.Line,
            Column = point.Offset,
            Buffer = testfile1.Content.Code
        };
        SignatureHelpResponse actual = await host.GetResponse<SignatureHelpRequest, SignatureHelpResponse>(OmniSharpEndpoints.SignatureHelp, request).ConfigureAwait(true);
        Assert.Single(actual.Signatures);
        Assert.Equal(1, actual.ActiveParameter);
        Assert.Equal(0, actual.ActiveSignature);
        Assert.Equal("A", actual.Signatures.ElementAt(0).Name);
        Assert.Equal(2, actual.Signatures.ElementAt(0).Parameters.Count());
    }

    [Fact]
    public async Task HandlesMiscFileDeletionAsync()
    {
        //When the file is deleted the diagnostics must not be returned
        using ITestProject testProject = await TestAssets.Instance.GetTestProjectAsync("EmptyProject").ConfigureAwait(true);
        var testfile = new TestFile("a.cs", "class C { b a = new b(); int n  }");
        using OmniSharpTestHost host = CreateOmniSharpHost(testProject.Directory);
        string filePath = await AddTestFileAsync(host, testProject, testfile).ConfigureAwait(true);
        var request = new CodeCheckRequest() { FileName = filePath };
        QuickFixResponse actual = await host.GetResponse<CodeCheckRequest, QuickFixResponse>(OmniSharpEndpoints.CodeCheck, request).ConfigureAwait(true);
        Assert.Single(actual.QuickFixes);
        await WaitForFileUpdateAsync(filePath, host, FileWatching.FileChangeType.Delete).ConfigureAwait(true);
        actual = await host.GetResponse<CodeCheckRequest, QuickFixResponse>(OmniSharpEndpoints.CodeCheck, request).ConfigureAwait(true);
        Assert.Empty(actual.QuickFixes);
    }

    private static async Task<string> AddTestFileAsync(OmniSharpTestHost host, ITestProject testProject, TestFile testfile)
    {
        string filePath = testProject.AddDisposableFile(testfile.FileName, testfile.Content.Text.ToString());
        await host.Workspace.BufferManager.UpdateBufferAsync(new Request() { FileName = filePath, Buffer = testfile.Content.Text.ToString() }).ConfigureAwait(true);
        return filePath;
    }

    private static async Task WaitForFileUpdateAsync(string filePath, OmniSharpTestHost host, FileWatching.FileChangeType changeType = FileWatching.FileChangeType.Create)
    {
        OnFilesChangedService fileChangedService = host.GetRequestHandler<OnFilesChangedService>(OmniSharpEndpoints.FilesChanged);
        await fileChangedService.Handle(new[]
        {
                new FilesChangedRequest
                {
                    FileName = filePath,
                    ChangeType = changeType
                }
            }).ConfigureAwait(true);

        await Task.Delay(2000).ConfigureAwait(true);
    }
}
