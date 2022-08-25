namespace System.CommandLine.Minimal;

public class OptionBuilder<T>
{
    internal OptionBuilder(Option<T> opt)
    {
        Option = opt;
    }

    internal Option<T> Option;

    public OptionBuilder<T> AddDescription(string description)
    {
        Option.Description = description;
        return this;
    }
    public OptionBuilder<T> AddAlias(params string[] aliases)
    {
        foreach(var alias in aliases)
            Option.AddAlias(alias);
        return this;
    }
    public OptionBuilder<T> AddDefaultValue(T value)
    {
        Option.SetDefaultValue(value);
        return this;
    }
    public OptionBuilder<T> AddDefaultValueFactory(Func<object?> factory)
    {
        Option.SetDefaultValueFactory(factory);
        return this;
    }
}
