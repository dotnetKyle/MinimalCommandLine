using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

public class MinimalCommandLineBuilder
{
    public MinimalCommandLineBuilder()
    {
        RootCommand = new RootCommand();
    }

    internal RootCommand RootCommand { get; private set; }

    public async Task ExecuteAsync(string[] args)
    {
        await RootCommand.InvokeAsync(args);
    }
    public int Execute(string[] args)
    {
        return RootCommand.Invoke(args);
    }

    public MinimalCommandLineBuilder AddRootDescription(string desc)
    {
        RootCommand.Description = desc;
        return this;
    }
    public MinimalCommandLineBuilder AddRootAlias(string alias)
    {
        RootCommand.AddAlias(alias);
        return this;
    }
    public MinimalCommandLineBuilder AddRootArgument<T>(string name)
    {
        var arg = new Argument<T>(name);
        RootCommand.AddArgument(arg);
        return this;
    }
    public MinimalCommandLineBuilder AddRootOption<T>(string name)
    {
        var opt = new Option<T>(name);
        RootCommand.AddOption(opt);
        return this;
    }

    public MinimalCommandLineBuilder AddCommand(string commandName, Action<CommandOptions> cmdOptions)
    {
        var cmd = new Command(commandName);
        var opt = new CommandOptions(cmd);        
        cmdOptions(opt);
        RootCommand.AddCommand(opt.Command);        
        return this;
    }
}
