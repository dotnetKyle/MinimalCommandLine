namespace System.CommandLine.Minimal;

public class ArgumentOptions<T>
{
    internal ArgumentOptions(Argument<T> arg)
    {
        Argument = arg;
    }
    internal Argument<T> Argument;

    public ArgumentOptions<T> AddDescription(string description)
    {
        Argument.Description = description;
        return this;
    }
    public ArgumentOptions<T> AddHelpName(string helpName)
    {
        Argument.HelpName = helpName;
        return this;
    }
    public ArgumentOptions<T> AddCompletions(params string[] completions)
    {
        Argument.Completions.Add(completions);
        return this;
    }
    public ArgumentOptions<T> AddArity(ArgumentArity arity)
    {
        Argument.Arity = arity;
        return this;
    }
    public ArgumentOptions<T> SetDefaultValue(T defaultValue)
    {
        Argument.SetDefaultValue(defaultValue);
        return this;
    }
}
