using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OmniSharp.Http.Middleware;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Models.GotoDefinition;
using OmniSharp.Models.v1;
using OmniSharp.Models.v1.FindSymbols;
using OmniSharp.Models.V1.UpdateBuffer;
using OmniSharp.Models.V1.WorkspaceInformation;
using OmniSharp.Roslyn;
using OmniSharp.Services;
using OmniSharp.Utilities;
using TestUtility;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.Http.Tests;

public class EndpointMiddlewareFacts : AbstractTestFixture
{
    public EndpointMiddlewareFacts(ITestOutputHelper output)
        : base(output)
    {
    }

    [OmniSharpHandler(OmniSharpEndpoints.GotoDefinition, LanguageNames.CSharp)]
    private class GotoDefinitionService : IRequestHandler<GotoDefinitionRequest, GotoDefinitionResponse>
    {
        [Import]
        public OmniSharpWorkspace Workspace { get; set; }

        public Task<GotoDefinitionResponse> Handle(GotoDefinitionRequest request) => Task.FromResult<GotoDefinitionResponse>(null);
    }

    [OmniSharpHandler(OmniSharpEndpoints.FindSymbols, LanguageNames.CSharp)]
    private class FindSymbolsService : IRequestHandler<FindSymbolsRequest, QuickFixResponse>
    {
        [Import]
        public OmniSharpWorkspace Workspace { get; set; }

        public Task<QuickFixResponse> Handle(FindSymbolsRequest request) => Task.FromResult<QuickFixResponse>(null);
    }

    [OmniSharpHandler(OmniSharpEndpoints.UpdateBuffer, LanguageNames.CSharp)]
    private class UpdateBufferService : IRequestHandler<UpdateBufferRequest, object>
    {
        [Import]
        public OmniSharpWorkspace Workspace { get; set; }

        public Task<object> Handle(UpdateBufferRequest request) => Task.FromResult<object>(null);
    }

    private class Response { }

    [Export(typeof(IProjectSystem))]
    private class FakeProjectSystem : IProjectSystem
    {
        public string Key { get; } = "Fake";
        public string Language { get; } = LanguageNames.CSharp;
        public IEnumerable<string> Extensions { get; } = new[] { ".cs" };
        public bool EnabledByDefault { get; } = true;
        public bool Initialized { get; } = true;
        public Task WaitForIdleAsync() => throw new NotImplementedException();
        public Task<object> GetWorkspaceModelAsync(WorkspaceInformationRequest request) => throw new NotImplementedException();
        public Task<object> GetProjectModelAsync(string path) => throw new NotImplementedException();
        public void Initalize(IConfiguration configuration) { }
    }

    private class PlugInHost : DisposableObject
    {
        private readonly IServiceProvider _serviceProvider;
        public CompositionHost CompositionHost { get; }

        public PlugInHost(IServiceProvider serviceProvider, CompositionHost compositionHost)
        {
            _serviceProvider = serviceProvider;
            CompositionHost = compositionHost;
        }

        protected override void DisposeCore(bool disposing)
        {
            (_serviceProvider as IDisposable)?.Dispose();
            CompositionHost.Dispose();
        }
    }

    static protected Assembly GetAssembly<T>() => typeof(T).GetTypeInfo().Assembly;

    private PlugInHost CreatePlugInHost(params Assembly[] assemblies)
    {
        IServiceProvider serviceProvider = TestServiceProvider.Create(TestOutput, new OmniSharpEnvironment());
        CompositionHost? compositionHost = new CompositionHostBuilder(serviceProvider)
            .WithAssemblies(assemblies)
            .Build(workingDirectory: null);

        return new PlugInHost(serviceProvider, compositionHost);
    }

    [Fact]
    public async Task PassesThroughForInvalidPathAsync()
    {
        RequestDelegate Next = _ => Task.Run(() => { throw new NotImplementedException(); });

        using PlugInHost host = CreatePlugInHost(GetAssembly<EndpointMiddlewareFacts>());
        var middleware = new EndpointMiddleware(Next, host.CompositionHost, LoggerFactory);
        var context = new DefaultHttpContext();
        context.Request.Path = PathString.FromUriComponent(new Uri("/notvalid", UriKind.Relative));

        await Assert.ThrowsAsync<NotImplementedException>(() => middleware.Invoke(context)).ConfigureAwait(true);
    }

    [Fact]
    public async Task DoesNotThrowForValidPathAsync()
    {
        RequestDelegate Next = _ => Task.Run(() => { throw new NotImplementedException(); });

        using PlugInHost host = CreatePlugInHost(GetAssembly<EndpointMiddlewareFacts>(), GetAssembly<OmniSharpEndpointMetadata>());
        EndpointMiddleware middleware = new(Next, host.CompositionHost, LoggerFactory);

        DefaultHttpContext context = new();
        context.Request.Path = PathString.FromUriComponent(new Uri("/gotodefinition", UriKind.Relative));

        context.Request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new GotoDefinitionRequest
                {
                    FileName = "bar.cs",
                    Line = 2,
                    Column = 14,
                    Timeout = 60000
                })
            )
        );
        await middleware.Invoke(context).ConfigureAwait(true);
        Assert.True(true);
    }

    [Fact]
    public async Task PassesThroughToServicesAsync()
    {
        RequestDelegate Next = _ => Task.Run(() => { throw new NotImplementedException(); });

        using PlugInHost host = CreatePlugInHost(GetAssembly<EndpointMiddlewareFacts>(), GetAssembly<OmniSharpEndpointMetadata>());
        EndpointMiddleware middleware = new(Next, host.CompositionHost, LoggerFactory);

        DefaultHttpContext context = new();
        context.Request.Path = PathString.FromUriComponent(new Uri("/gotodefinition", UriKind.Relative));

        context.Request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new GotoDefinitionRequest
                {
                    FileName = "bar.cs",
                    Line = 2,
                    Column = 14,
                    Timeout = 60000
                })
            )
        );
        await middleware.Invoke(context).ConfigureAwait(true);
        Assert.True(true);
    }

    [Fact]
    public async Task PassesThroughToAllServicesWithDelegateAsync()
    {
        RequestDelegate Next = _ => Task.Run(() => { throw new NotImplementedException(); });

        using (var host = CreatePlugInHost(
            GetAssembly<EndpointMiddlewareFacts>(),
            GetAssembly<OmniSharpEndpointMetadata>()))
        {
            var middleware = new EndpointMiddleware(Next, host.CompositionHost, LoggerFactory);

            var context = new DefaultHttpContext();
            context.Request.Path = PathString.FromUriComponent(new Uri("/findsymbols", UriKind.Relative));

            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new FindSymbolsRequest
                    {

                    })
                )
            );

            await middleware.Invoke(context).ConfigureAwait(true);

            Assert.True(true);
        }
    }

    [Fact]
    public async Task PassesThroughToSpecificServiceWithDelegateAsync()
    {
        RequestDelegate Next = _ => Task.Run(() => { throw new NotImplementedException(); });

        using PlugInHost host = CreatePlugInHost(
        typeof(EndpointMiddlewareFacts).GetTypeInfo().Assembly,
        typeof(OmniSharpEndpointMetadata).GetTypeInfo().Assembly);
        var middleware = new EndpointMiddleware(Next, host.CompositionHost, LoggerFactory);
        var context = new DefaultHttpContext();
        context.Request.Path = PathString.FromUriComponent(new Uri("/findsymbols", UriKind.Relative));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new FindSymbolsRequest { Language = LanguageNames.CSharp })));
        await middleware.Invoke(context).ConfigureAwait(true);
        Assert.True(true);
    }

    [OmniSharpEndpoint("/throw", typeof(ThrowRequest), typeof(ThrowResponse))]
    private class ThrowRequest : IRequest { }
    private class ThrowResponse { }

    [Fact]
    public async Task ShouldThrowIfTypeIsNotMergeableAsync()
    {
        RequestDelegate Next = async (ctx) => await Task.Run(() => { throw new NotImplementedException(); }).ConfigureAwait(true);
        using PlugInHost host = CreatePlugInHost(GetAssembly<EndpointMiddlewareFacts>());
        var middleware = new EndpointMiddleware(Next, host.CompositionHost, LoggerFactory);
        var context = new DefaultHttpContext();
        context.Request.Path = PathString.FromUriComponent(new Uri("/throw", UriKind.Relative));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ThrowRequest())));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await middleware.Invoke(context).ConfigureAwait(true)).ConfigureAwait(true);
    }
}
