using Newtonsoft.Json;
using System;

namespace OmniSharp.Options;

public record OmniSharpOptions
(
    RoslynExtensionsOptions RoslynExtensionsOptions,
    FormattingOptions FormattingOptions,
    FileOptions FileOptions,
    RenameOptions RenameOptions,
    ImplementTypeOptions ImplementTypeOptions,
    DotNetCliOptions DotNetCliOptions,
    OmniSharpExtensionsOptions Plugins
)
{
    public override string ToString() => JsonConvert.SerializeObject(this);
}
