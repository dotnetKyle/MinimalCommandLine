namespace System.CommandLine.Minimal;

public class ArgumentBuilder<T>
{
    internal ArgumentBuilder(Argument<T> arg)
    {
        Argument = arg;
    }
    internal Argument<T> Argument;

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
        Argument.Completions.Add(completions);
        return this;
    }
    public ArgumentBuilder<T> AddArity(ArgumentArity arity)
    {
        Argument.Arity = arity;
        return this;
    }
    public ArgumentBuilder<T> SetDefaultValue(T defaultValue)
    {
        Argument.SetDefaultValue(defaultValue);
        return this;
    }
}
