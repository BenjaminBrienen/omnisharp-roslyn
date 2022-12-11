using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OmniSharp.Endpoint;
using OmniSharp.Mef;
using OmniSharp.Plugins;
using OmniSharp.Services;
using OmniSharp.Protocol;
using OmniSharp.Models.V1.UpdateBuffer;
using OmniSharp.Models;

namespace OmniSharp.Http.Middleware;

internal class EndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _endpoints;
    private readonly IReadOnlyDictionary<string, Lazy<EndpointHandler>> _endpointHandlers;
    private readonly CompositionHost _host;
    private readonly ILogger _logger;
    private readonly IEnumerable<IProjectSystem> _projectSystems;

    public EndpointMiddleware(RequestDelegate next, CompositionHost host, ILoggerFactory loggerFactory)
    {
        _next = next;
        _host = host;
        _projectSystems = host.GetExports<IProjectSystem>();
        _logger = loggerFactory.CreateLogger<EndpointMiddleware>();
        IEnumerable<OmniSharpEndpointMetadata> endpoints = _host.GetExports<Lazy<IRequest, OmniSharpEndpointMetadata>>()
            .Select(x => x.Metadata);

        IEnumerable<Lazy<IRequestHandler, OmniSharpRequestHandlerMetadata>> handlers = _host.GetExports<Lazy<IRequestHandler, OmniSharpRequestHandlerMetadata>>();

        _endpoints = new HashSet<string>(
                endpoints
                    .Select(x => x.EndpointName)
                    .Distinct(),
                StringComparer.OrdinalIgnoreCase
            );

        var updateBufferEndpointHandler = new Lazy<EndpointHandler<UpdateBufferRequest, object>>(() => (EndpointHandler<UpdateBufferRequest, object>)_endpointHandlers[OmniSharpEndpoints.UpdateBuffer].Value);
        var languagePredicateHandler = new LanguagePredicateHandler(_projectSystems);
        var projectSystemPredicateHandler = new StaticLanguagePredicateHandler("Projects");
        var nugetPredicateHandler = new StaticLanguagePredicateHandler("NuGet");
        var endpointHandlers = endpoints.ToDictionary(
                x => x.EndpointName,
                endpoint => new Lazy<EndpointHandler>(() =>
                {
                    IPredicateHandler handler;

                    // Projects are a special case, this allows us to select the correct "Projects" language for them
                    if (endpoint.EndpointName is OmniSharpEndpoints.ProjectInformation or OmniSharpEndpoints.WorkspaceInformation)
                        handler = projectSystemPredicateHandler;
                    else handler = endpoint.EndpointName is OmniSharpEndpoints.PackageSearch or OmniSharpEndpoints.PackageSource or OmniSharpEndpoints.PackageVersion
                        ? nugetPredicateHandler
                        : languagePredicateHandler;

                    // This lets any endpoint, that contains a Request object, invoke update buffer.
                    // The language will be same language as the caller, this means any language service
                    // must implement update buffer.
                    Lazy<EndpointHandler<UpdateBufferRequest, object>> updateEndpointHandler = updateBufferEndpointHandler;
                    if (endpoint.EndpointName == OmniSharpEndpoints.UpdateBuffer)
                    {
                        // We don't want to call update buffer on update buffer.
                        updateEndpointHandler = new Lazy<EndpointHandler<UpdateBufferRequest, object>>(() => null);
                    }

                    return EndpointHandler.Factory(handler, _host, _logger, endpoint, handlers, updateEndpointHandler, Enumerable.Empty<Plugin>());
                }),
                StringComparer.OrdinalIgnoreCase
            );

        _endpointHandlers = new ReadOnlyDictionary<string, Lazy<EndpointHandler>>(endpointHandlers);
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Path.HasValue)
        {
            string endpoint = httpContext.Request.Path.Value;
            if (_endpoints.Contains(endpoint))
            {
                if (_endpointHandlers.TryGetValue(endpoint, out Lazy<EndpointHandler> handler))
                {
                    object response = await handler.Value.Handle(new RequestPacket()
                    {
                        Command = endpoint,
                        ArgumentsStream = httpContext.Request.Body
                    }).ConfigureAwait(false);

                    httpContext.Response.WriteJson(response);
                    return;
                }
            }
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
