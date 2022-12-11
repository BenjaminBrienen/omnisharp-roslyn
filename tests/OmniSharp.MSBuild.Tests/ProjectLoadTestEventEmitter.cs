using Microsoft.Extensions.Logging;
using OmniSharp.Eventing;
using OmniSharp.Mef;
using OmniSharp.Models.Events;
using OmniSharp.MSBuild.Notification;
using System;
using System.Collections.Immutable;
using System.Composition.Hosting.Core;
using System.Threading;

namespace OmniSharp.MSBuild.Tests;

public partial class ProjectLoadListenerTests
{
    private sealed class ProjectLoadTestEventEmitter : IEventEmitter, IDisposable
    {
        public ImmutableArray<ProjectConfigurationMessage> ReceivedMessages { get; private set; } = ImmutableArray<ProjectConfigurationMessage>.Empty;
        private readonly ManualResetEvent _messageEvent = new(false);

        public void WaitForProjectUpdate()
        {
            _messageEvent.Reset();
            _messageEvent.WaitOne(TimeSpan.FromSeconds(5));
        }

        public ExportDescriptorProvider[] AsExportDescriptionProvider(ILoggerFactory loggerFactory)
        {
            var listener = new ProjectLoadListener(loggerFactory, this);

            return new ExportDescriptorProvider[]
            {
                MefValueProvider.From<IMSBuildEventSink>(listener)
            };
        }

        public void Emit(string kind, object args)
        {
            if (args is ProjectConfigurationMessage projectConfigurationMessage)
            {
                ReceivedMessages = ReceivedMessages.Add(projectConfigurationMessage);
                _messageEvent.Set();
            }
        }

        public void Dispose()
        {
            _messageEvent.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
