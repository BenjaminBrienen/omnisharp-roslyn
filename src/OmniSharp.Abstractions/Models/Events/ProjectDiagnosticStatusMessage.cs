namespace OmniSharp.Models.Events;

public record ProjectDiagnosticStatusMessage
(
    ProjectDiagnosticStatus Status,
    string ProjectFilePath,
    string Type
);
