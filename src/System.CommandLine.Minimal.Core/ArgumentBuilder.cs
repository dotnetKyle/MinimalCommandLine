namespace System.CommandLine.Minimal;

public class ArgumentBuilder<T>
{
    internal ArgumentBuilder(string name, Argument<T> arg)
    {
        Argument = arg;
        Name = name;
    }

    internal Argument<T> Argument { get; private set; }
    internal string Name { get; private set; }

    public ArgumentBuilder<T> AddDescription(string description)
    {
        Argument.Description = description;
        return this;
    }

    public ArgumentBuilder<T> AddHelpName(string helpName)
    {
        Argument.HelpName = helpName;
        return this;
    }

    public ArgumentBuilder<T> AddCompletions(params string[] completions)
    {
        foreach (var completion in completions)
        {
            Argument.CompletionSources.Add(completion);
        }
        return this;
    }

    public ArgumentBuilder<T> AddArity(ArgumentArity arity)
    {
        Argument.Arity = arity;
        return this;
    }

    public ArgumentBuilder<T> SetDefaultValue(T defaultValue)
    {
        Argument.DefaultValueFactory = _ => defaultValue;
        return this;
    }
}
