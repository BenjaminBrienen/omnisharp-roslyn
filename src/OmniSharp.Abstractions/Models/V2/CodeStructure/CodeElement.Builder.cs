using System.Collections.Immutable;

namespace OmniSharp.Models.V2.CodeStructure;

public partial record CodeElement
{
#pragma warning disable CA1034
    public record Builder(string Kind, string Name, string DisplayName)
    {
        private readonly ImmutableList<CodeElement>.Builder _childrenBuilder = ImmutableList.CreateBuilder<CodeElement>();
        private readonly ImmutableDictionary<string, Range>.Builder _rangesBuilder = ImmutableDictionary.CreateBuilder<string, Range>();
        private readonly ImmutableDictionary<string, object>.Builder _propertiesBuilder = ImmutableDictionary.CreateBuilder<string, object>();

        public void AddChild(CodeElement element) => _childrenBuilder.Add(element);
        public void AddRange(string name, Range range) => _rangesBuilder.Add(name, range);
        public void AddProperty(string name, object value) => _propertiesBuilder.Add(name, value);

        public CodeElement ToCodeElement()
        {
            return new CodeElement(
                Kind,
                Name,
                DisplayName,
                _childrenBuilder.ToImmutable(),
                _rangesBuilder.ToImmutable(),
                _propertiesBuilder.ToImmutable());
        }
    }
}
