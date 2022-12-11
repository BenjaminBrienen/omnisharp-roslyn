using System;
using System.Collections.Generic;
using OmniSharp.Models.Events;
using OmniSharp.Models;
using System.Linq;

namespace OmniSharp.Eventing;

public static class IEventEmitterExtensions
{
    public static void Error(this IEventEmitter emitter, Exception ex, string? fileName = null)
    {
        if (emitter is null)
            throw new ArgumentNullException(nameof(emitter));
        if (ex is null)
            throw new ArgumentNullException(nameof(ex));
        emitter.Emit(EventTypes.Error, new ErrorMessage(fileName, ex.ToString()));
    }

    public static void RestoreStarted(this IEventEmitter emitter, string projectPath)
    {
        if (emitter is null)
            throw new ArgumentNullException(nameof(emitter));
        emitter.Emit(
            EventTypes.PackageRestoreStarted,
            new PackageRestoreMessage { FileName = projectPath });
    }

    public static void RestoreFinished(this IEventEmitter emitter, string projectPath, bool succeeded)
    {
        if (emitter is null)
            throw new ArgumentNullException(nameof(emitter));
        emitter.Emit(
            EventTypes.PackageRestoreFinished,
            new PackageRestoreMessage
            {
                FileName = projectPath,
                Succeeded = succeeded
            });
    }

    public static void UnresolvedDepdendencies(this IEventEmitter emitter, string projectFilePath, IEnumerable<PackageDependency> unresolvedDependencies)
    {
        if (emitter is null)
            throw new ArgumentNullException(nameof(emitter));
        emitter.Emit(
            EventTypes.UnresolvedDependencies,
            new UnresolvedDependenciesMessage
            {
                FileName = projectFilePath,
                UnresolvedDependencies = unresolvedDependencies
            });
    }

    public static void ProjectInformation(
        this IEventEmitter emitter,
        HashedString projectId,
        HashedString sessionId,
        int outputKind,
        IEnumerable<string> projectCapabilities,
        IEnumerable<string> targetFrameworks,
        HashedString sdkVersion,
        IEnumerable<HashedString> references,
        IEnumerable<HashedString> fileExtensions,
        IEnumerable<int> fileCounts)
    {
        if (emitter is null)
            throw new ArgumentNullException(nameof(emitter));
        if (projectId is null)
            throw new ArgumentNullException(nameof(projectId));
        if (sessionId is null)
            throw new ArgumentNullException(nameof(sessionId));
        if (sdkVersion is null)
            throw new ArgumentNullException(nameof(sdkVersion));
        var projectConfiguration = new ProjectConfigurationMessage
        (
            ProjectCapabilities: projectCapabilities,
            TargetFrameworks: targetFrameworks,
            SdkVersion: sdkVersion.Value,
            OutputKind: outputKind,
            ProjectId: projectId.Value,
            SessionId: sessionId.Value,
            References: references.Select(hashed => hashed.Value),
            FileExtensions: fileExtensions.Select(hashed => hashed.Value),
            FileCounts: fileCounts
        );

        emitter.Emit(
            EventTypes.ProjectConfiguration,
            projectConfiguration);
    }
}
