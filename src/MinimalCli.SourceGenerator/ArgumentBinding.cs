using MinimalCli.SourceGeneration.Conventions;

namespace MinimalCli.SourceGeneration;

internal record ArgumentBinding(string OriginalParameterName, string Type, string? DefaultValueConstant, bool IsCollectionType)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant, IsCollectionType)
{
    /// <summary>
    /// The Argument name in title case, e.g. "myParameterName" becomes "My Parameter Name"
    /// </summary>
    public string ConventionalArgumentName 
        => ParameterNameConversion.ToArgumentName(this.OriginalParameterName);
}
