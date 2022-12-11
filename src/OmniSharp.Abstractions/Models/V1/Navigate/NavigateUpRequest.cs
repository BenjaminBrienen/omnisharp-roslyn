using System.Collections.Generic;
using OmniSharp.Mef;
using OmniSharp.Models.Navigate;

namespace OmniSharp.Models.V1.Navigate;

[OmniSharpEndpoint(OmniSharpEndpoints.NavigateUp, typeof(NavigateUpRequest), typeof(NavigateResponse))]
public record NavigateUpRequest(
int Line,
int Column,
string Buffer,
IEnumerable<LinePositionSpanTextChange> Changes,
bool ApplyChangesTogether,
string FileName)
: Request(Line, Column, Buffer, Changes, ApplyChangesTogether, FileName);
