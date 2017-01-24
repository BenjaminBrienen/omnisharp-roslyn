﻿using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using OmniSharp.Models;
using OmniSharp.Options;
using OmniSharp.Roslyn.CSharp.Services.Intellisense;
using TestUtility;
using Xunit.Abstractions;

namespace OmniSharp.Roslyn.CSharp.Tests
{
    public class AbstractAutoCompleteTests : AbstractTestFixture
    {
        protected AbstractAutoCompleteTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override IEnumerable<Assembly> GetHostAssemblies()
        {
            yield return GetAssembly<IntellisenseService>();
        }

        protected async Task<IEnumerable<AutoCompleteResponse>> FindCompletionsAsync(string source, AutoCompleteRequest request = null, bool wantSnippet = false)
        {
            var testFile = new TestFile("dummy.cs", source);
            var markup = TestContent.Parse(source);

            var workspace = await CreateWorkspaceAsync(testFile);
            var controller = new IntellisenseService(workspace, new FormattingOptions());

            if (request == null)
            {
                request = CreateRequest(source, wantSnippet: wantSnippet);
            }

            return await controller.Handle(request);
        }

        protected AutoCompleteRequest CreateRequest(string source, bool wantSnippet = false)
        {
            var testFile = new TestFile("dummy.cs", source);
            var point = testFile.Content.GetPointFromPosition();

            return new AutoCompleteRequest
            {
                Line = point.Line,
                Column = point.Offset,
                FileName = testFile.FileName,
                Buffer = testFile.Content.Code,
                WordToComplete = GetPartialWord(testFile.Content),
                WantMethodHeader = true,
                WantSnippet = wantSnippet,
                WantReturnType = true
            };
        }

        private static string GetPartialWord(TestContent testConnect)
        {
            if (!testConnect.HasPosition || testConnect.Position == 0)
            {
                return string.Empty;
            }

            var index = testConnect.Position;
            while (index >= 1)
            {
                var ch = testConnect.Code[index - 1];
                if (ch != '_' && !char.IsLetterOrDigit(ch))
                {
                    break;
                }

                index--;
            }

            return testConnect.Code.Substring(index, testConnect.Position - index);
        }
    }
}
