#nullable enable

using OmniSharp.Models.Metadata;
using OmniSharp.Models.V1.SourceGeneratedFile;
using System.Collections.Generic;

namespace OmniSharp.Models.V2.GotoTypeDefinition;

public record GotoTypeDefinitionResponse(IEnumerable<TypeDefinition>? Definitions);

public record TypeDefinition
(
    Location Location,
    MetadataSource? MetadataSource,
    SourceGeneratedFileInfo? SourceGeneratedFileInfo
);
