using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using OmniSharp.Cake.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Roslyn;

namespace OmniSharp.Cake.Services.RequestHandlers;

public abstract class CakeRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
{
    private string? _endpointName;
    public string EndpointName
    {
        get
        {
            if (string.IsNullOrEmpty(_endpointName))
            {
                _endpointName = GetType().GetTypeInfo().GetCustomAttribute<OmniSharpHandlerAttribute>()?.EndpointName
                    ?? throw new InvalidOperationException($"Missing {nameof(OmniSharpHandlerAttribute)} on {this} {GetType().FullName}");
            }
            return _endpointName;
        }
    }

    [ImportMany]
    public IEnumerable<Lazy<IRequestHandler, OmniSharpRequestHandlerMetadata>>? Handlers { get; set; }
    public OmniSharpWorkspace Workspace { get; }
    public Lazy<IRequestHandler<TRequest, TResponse>> Service { get; }

    protected CakeRequestHandler(OmniSharpWorkspace workspace)
    {
        Workspace = workspace;
        Service = new Lazy<IRequestHandler<TRequest, TResponse>>(() =>
        {
            return (IRequestHandler<TRequest, TResponse>?)Handlers?.FirstOrDefault(requestHandler
                => requestHandler.Metadata.EndpointName.Equals(EndpointName, StringComparison.Ordinal)
                    && requestHandler.Metadata.Language.Equals(LanguageNames.CSharp, StringComparison.Ordinal))?
                    .Value ?? throw new InvalidOperationException($"Couldn't construct Service on {GetType().FullName}. {nameof(Handlers)}: {Handlers}");
        });
    }

    public virtual async Task<TResponse> Handle(TRequest request)
    {
        IRequestHandler<TRequest, TResponse> service = Service.Value;
        if (service is null)
        {
            throw new NotSupportedException();
        }
        request = await TranslateRequestAsync(request).ConfigureAwait(false);
        if (!IsValid(request))
        {
            throw new InvalidOperationException("Request is not valid.");
        }
        TResponse response = await service.Handle(request).ConfigureAwait(false);
        return await TranslateResponse(response, request).ConfigureAwait(false);
    }

    protected virtual bool IsValid(TRequest request) => true;

    protected virtual async Task<TRequest> TranslateRequestAsync(TRequest req)
    {
        var request = req as Request;

        if (request is not null)
        {
            await request.TranslateAsync(Workspace).ConfigureAwait(false);
        }

        return req;
    }

    protected virtual Task<TResponse> TranslateResponse(TResponse response, TRequest request) => Task.FromResult(response);
}
