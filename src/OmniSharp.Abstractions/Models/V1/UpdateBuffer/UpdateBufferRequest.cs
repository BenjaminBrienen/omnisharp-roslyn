using System.Collections.Generic;
using OmniSharp.Mef;

namespace OmniSharp.Models.V1.UpdateBuffer;

[OmniSharpEndpoint(OmniSharpEndpoints.UpdateBuffer, typeof(UpdateBufferRequest), typeof(object))]
public record UpdateBufferRequest(
    bool FromDisk, // Instead of updating the buffer from the editor, set this to allow updating from disk.
    int Line,
    int Column,
    string Buffer,
    IEnumerable<LinePositionSpanTextChange> Changes,
    bool ApplyChangesTogether,
    string FileName) : Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
