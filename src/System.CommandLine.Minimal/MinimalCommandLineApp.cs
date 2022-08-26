using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

public class MinimalCommandLineApp
{
    public MinimalCommandLineApp(IServiceProvider services, IConfigurationRoot configuration)
    {
        Services = services;
        Configuration = configuration;

        RootCommand = new RootCommand();
    }

    public IServiceProvider Services { get; private set; }
    public IConfigurationRoot Configuration { get; private set; }

    internal RootCommand RootCommand { get; private set; }

    public async Task ExecuteAsync(string[] args)
    {
        await RootCommand.InvokeAsync(args);
    }
    public int Execute(string[] args)
    {
        return RootCommand.Invoke(args);
    }

    public MinimalCommandLineApp AddRootDescription(string desc)
    {
        RootCommand.Description = desc;
        return this;
    }
    public MinimalCommandLineApp AddRootAlias(string alias)
    {
        RootCommand.AddAlias(alias);
        return this;
    }
    public MinimalCommandLineApp AddRootArgument<T>(string name)
    {
        var arg = new Argument<T>(name);
        RootCommand.AddArgument(arg);
        return this;
    }
    public MinimalCommandLineApp AddRootOption<T>(string name)
    {
        var opt = new Option<T>(name);
        RootCommand.AddOption(opt);
        return this;
    }

    public MinimalCommandLineApp MapCommand<THandler>(
        string commandName,
        Func<THandler, Delegate> handler,
        Action<CommandBuilder<THandler>> cmdOptions
        ) 
        where THandler : notnull
    {
        var cmd = new Command(commandName);
        var builder = new CommandBuilder<THandler>(cmd, Services, handler);
        cmdOptions(builder);
        cmd.SetHandler(builder.handlerActivator);

        RootCommand.AddCommand(builder.Command);

        return this;
    }

    public MinimalCommandLineApp AddCommand(string commandName, Action<CommandBuilder> cmdOptions)
    {
        var cmd = new Command(commandName);
        var opt = new CommandBuilder(cmd);                cmdOptions(opt);
        RootCommand.AddCommand(opt.Command);        
        return this;
    }


}
