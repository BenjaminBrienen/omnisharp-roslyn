namespace OmniSharp.Models.Events;

public record BackgroundDiagnosticStatusMessage
(
    BackgroundDiagnosticStatus Status,
    int NumberProjects,
    int NumberFilesTotal,
    int NumberFilesRemaining
);
