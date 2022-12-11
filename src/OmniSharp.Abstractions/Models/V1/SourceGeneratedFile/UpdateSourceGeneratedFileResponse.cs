#nullable enable

namespace OmniSharp.Models.V1.SourceGeneratedFile
{
    public record UpdateSourceGeneratedFileResponse
    {
        public UpdateType UpdateType { get; init; }
        public string? Source { get; init; }
    }

    public enum UpdateType
    {
        Unchanged,
        Deleted,
        Modified
    }
}
