namespace OmniSharp.Models.Events;

public record PackageRestoreMessage
(
    string FileName,
    bool Succeeded
);
