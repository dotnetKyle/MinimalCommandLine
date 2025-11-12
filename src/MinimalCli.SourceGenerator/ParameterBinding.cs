using MinimalCli.SourceGeneration.Conventions;

namespace MinimalCli.SourceGeneration;

internal abstract record ParameterBinding(string OriginalParameterName, string Type, string? DefaultValueConstant, bool IsCollectionType)
{
    public string NameTitleCase
    {
        get
        {
            return this.OriginalParameterName.ToSymbolName();
        }
    }
    public string HelpName
    {
        get
        {
            // split and add spaces
            char c = char.ToUpper(this.OriginalParameterName[0]);
            return c + this.OriginalParameterName.Substring(1);
        }
    }
}
