using System.CommandLine.Parsing;

namespace System.CommandLine.Minimal;

public class OptionBuilder<T>
{
    internal OptionBuilder(string name, Option<T> opt)
    {
        Option = opt;
        Name = name;
    }

    internal Option<T> Option { get; private set; }
    internal string Name { get; private set; }

    public OptionBuilder<T> AddDescription(string description)
    {
        Option.Description = description;
        return this;
    }

    public OptionBuilder<T> AddAlias(params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            Option.Aliases.Add(alias);
        }
        return this;
    }

    public OptionBuilder<T> AddDefaultValue(T value)
    {
        Option.DefaultValueFactory = _ => value;
        return this;
    }

    public OptionBuilder<T> AddDefaultValueFactory(Func<T> factory)
    {
        Option.DefaultValueFactory = _ => factory();
        return this;
    }
}
