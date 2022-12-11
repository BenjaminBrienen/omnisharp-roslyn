using System.Collections.Generic;
using OmniSharp.FileWatching;
using OmniSharp.Mef;
using OmniSharp.Models.FilesChanged;

namespace OmniSharp.Models.V1.FilesChanged;

[OmniSharpEndpoint(OmniSharpEndpoints.FilesChanged, typeof(IEnumerable<FilesChangedRequest>), typeof(FilesChangedResponse))]
public record FilesChangedRequest(
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName,
    FileChangeType ChangeType) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
