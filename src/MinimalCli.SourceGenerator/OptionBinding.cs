using MinimalCli.SourceGeneration.Conventions;

namespace MinimalCli.SourceGeneration;

internal record OptionBinding(string OriginalParameterName, string Type, string? DefaultValueConstant, bool IsCollectionType)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant, IsCollectionType)
{
    /// <summary>
    /// The option name in kebab case, e.g. "myParameterName" becomes "--my-parameter-name"
    /// </summary>
    public string OptionName => ParameterNameConversion.ToOptionName(this.OriginalParameterName);
}
