using System;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Models;
using OmniSharp.Models.V1.FixUsings;
using OmniSharp.Roslyn.CSharp.Services.Refactoring;
using TestUtility;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.Roslyn.CSharp.Tests;
public class FixUsingsFacts : AbstractSingleRequestHandlerTestFixture<FixUsingService>
{
    private const string TestFileName = "test.cs";

    public FixUsingsFacts(ITestOutputHelper output, SharedOmniSharpHostFixture sharedOmniSharpHostFixture)
        : base(output, sharedOmniSharpHostFixture)
    {
    }

    protected override string EndpointName => OmniSharpEndpoints.FixUsings;

    [Fact]
    public async Task FixUsingsAddsUsingSingleAsync()
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

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingSingleForFrameworkMethodAsync()
    {
        const string code = @"
namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
        }
    }
}";

        string expectedCode = @"
using System;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingSingleForFrameworkClassAsync()
    {
        const string code = @"
namespace OmniSharp
{
    public class class1
    {
        public void method1()()
        {
            var s = new StringBuilder();
        }
    }
}";

        const string expectedCode = @"
using System.Text;

namespace OmniSharp
{
    public class class1
    {
        public void method1()()
        {
            var s = new StringBuilder();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingMultipleAsync()
    {
        const string code = @"
namespace nsA
{
    public class classX{}
}

namespace nsB
{
    public class classY{}
}

namespace OmniSharp
{
    public class class1
    {
        public method1()
        {
            var c1 = new classX();
            var c2 = new classY();
        }
    }
}";

        const string expectedCode = @"
using nsA;
using nsB;

namespace nsA
{
    public class classX{}
}

namespace nsB
{
    public class classY{}
}

namespace OmniSharp
{
    public class class1
    {
        public method1()
        {
            var c1 = new classX();
            var c2 = new classY();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingMultipleForFrameworkAsync()
    {
        const string code = @"
namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
            var sb = new StringBuilder();
        }
    }
}";

        const string expectedCode = @"
using System;
using System.Text;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
            var sb = new StringBuilder();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsReturnsAmbiguousResultAsync()
    {
        const string code = @"
namespace nsA
{
    public class classX{}
}

namespace nsB
{
    public class classX{}
}

namespace OmniSharp
{
    public class class1
    {
        public method1()
        {
            var c1 = new $$classX();
        }
    }
}";
        var content = TestContent.Parse(code);
        TextPoint point = content.GetPointFromPosition();

        QuickFix[] expectedUnresolved = new[]
        {
            new QuickFix()
            {
                Line = point.Line,
                Column = point.Offset,
                FileName = TestFileName,
                Text = "`classX` is ambiguous. Namespaces: using nsA; using nsB;",
            }
        };

        await AssertUnresolvedReferencesAsync(content.Code, expectedUnresolved).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsReturnsNoUsingsForAmbiguousResultAsync()
    {
        const string code = @"
namespace nsA {
    public class classX{}
}

namespace nsB {
    public class classX{}
}

namespace OmniSharp {
    public class class1
    {
        public method1()
        {
            var c1 = new classX();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode: code).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingForExtensionAsync()
    {
        const string code = @"
namespace nsA {
    public static class StringExtension {
        public static void Whatever(this string astring) {}
    }
}

namespace OmniSharp {
    public class class1
    {
        public method1()
        {
            ""string"".Whatever();
        }
    }
}";

        const string expectedCode = @"
using nsA;

namespace nsA {
    public static class StringExtension {
        public static void Whatever(this string astring) {}
    }
}

namespace OmniSharp {
    public class class1
    {
        public method1()
        {
            ""string"".Whatever();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingLinqMethodSyntaxAsync()
    {
        const string code = @"namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            List<string> first = new List<string>();
            var testing = first.Where(s => s == ""abc"");
        }
    }
}";

        const string expectedCode = @"using System.Collections.Generic;
using System.Linq;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            List<string> first = new List<string>();
            var testing = first.Where(s => s == ""abc"");
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsAddsUsingLinqQuerySyntaxAsync()
    {
        const string code = @"namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
            var lowNums =
                from n in numbers
                where n < 5
                select n;
        }
     }
}";

        const string expectedCode = @"using System.Linq;
namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
            var lowNums =
                from n in numbers
                where n < 5
                select n;
        }
     }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsRemoveDuplicateUsingAsync()
    {
        const string code = @"
using System;
using System;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
        }
    }
}";

        const string expectedCode = @"
using System;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    [Fact]
    public async Task FixUsingsRemoveUnusedUsingAsync()
    {
        const string code = @"
using System;
using System.Linq;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
        }
    }
}";

        const string expectedCode = @"
using System;

namespace OmniSharp
{
    public class class1
    {
        public void method1()
        {
            Guid.NewGuid();
        }
    }
}";

        await AssertBufferContentsAsync(code, expectedCode).ConfigureAwait(true);
    }

    private async Task AssertBufferContentsAsync(string code, string expectedCode)
    {
        FixUsingsResponse response = await RunFixUsingsAsync(code).ConfigureAwait(true);
        Assert.Equal(FlattenNewLines(expectedCode), FlattenNewLines(response.Buffer));
    }

    private static string FlattenNewLines(string input) => input.Replace("\r\n", "\n", StringComparison.Ordinal);

    private async Task AssertUnresolvedReferencesAsync(string code, QuickFix[] expectedResults)
    {
        FixUsingsResponse response = await RunFixUsingsAsync(code).ConfigureAwait(true);
        QuickFix[] results = response.AmbiguousResults.ToArray();

        Assert.Equal(results.Length, expectedResults.Length);

        for (int i = 0; i < results.Length; i++)
        {
            QuickFix result = results[i];
            QuickFix expectedResult = expectedResults[i];

            Assert.Equal(expectedResult.Line, result.Line);
            Assert.Equal(expectedResult.Column, result.Column);
            Assert.Equal(expectedResult.FileName, result.FileName);
            Assert.Equal(expectedResult.Text, result.Text);
        }
    }

    private async Task<FixUsingsResponse> RunFixUsingsAsync(string code)
    {
        SharedOmniSharpTestHost.AddFilesToWorkspace(new TestFile(TestFileName, code));
        FixUsingService requestHandler = GetRequestHandler(SharedOmniSharpTestHost);
        var request = new FixUsingsRequest
        {
            FileName = TestFileName
        };

        return await requestHandler.Handle(request).ConfigureAwait(true);
    }
}
