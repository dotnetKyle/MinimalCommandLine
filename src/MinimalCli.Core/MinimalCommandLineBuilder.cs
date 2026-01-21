using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimalCli.Bindings;
using System.Linq;

namespace MinimalCli;

public class MinimalCommandLineBuilder : IHostApplicationBuilder
{ 
    private readonly string[] args;
    internal readonly HostApplicationBuilder builder;
    internal CommandExecutionMode cmdExecutionMode = CommandExecutionMode.CliDefault;

    public MinimalCommandLineBuilder(string[] args)
    {
        this.args = args ?? [];
        this.builder = Host.CreateApplicationBuilder();
        this.Properties = new Dictionary<object, object>();
    }
    
    public IServiceCollection Services => builder.Services;
    public IConfigurationManager Configuration => builder.Configuration;
    public IDictionary<object, object> Properties { get; }
    public IHostEnvironment Environment => builder.Environment;
    public ILoggingBuilder Logging => builder.Logging;
    public IMetricsBuilder Metrics => builder.Metrics;

    List<CommandOptions> commandOptionsCollection = new();
    public TOptions TryRegisterCommandOptions<TOptions>()
        where TOptions : CommandOptions, new()
    {
        // this needs to be idempotent so that the CommandOptions won't accidently get registered more than once.
        foreach(CommandOptions options in this.commandOptionsCollection)
        {
            if(options is TOptions alreadyConfiguredOptions)
                return alreadyConfiguredOptions;
        }

        // source generated TOptions all have empty constructors
        TOptions newOptions = new();

        this.commandOptionsCollection.Add(newOptions);

        return newOptions;
    }

    internal RootCommand? RootCommand { get; private set; }
    public MinimalCommandLineApp Build()
    {
        // add required services
        this.Services.AddSingleton<CommandExecutorCli>();
        this.Services.AddSingleton<CommandExecutorShell>();

        // create and add this instance to DI
        CommandBindingFactory cmdBindingFactory = new();
        this.Services.AddSingleton(cmdBindingFactory);

        // check if there was a generated root command
        CommandOptions? rootOptions = commandOptionsCollection.FirstOrDefault(opt => opt.Command is RootCommand);
        if (rootOptions is not null)
        {
            this.RootCommand = (RootCommand)rootOptions.Command;
            cmdBindingFactory.AddRootCommand(rootOptions);
            commandOptionsCollection.Remove(rootOptions);
            ParameterBinding[] bindings = rootOptions.SetupCommandParameterBindings();
        }
        else
        {
            this.RootCommand = new();
        }

        foreach (CommandOptions commandOptions in commandOptionsCollection)
        {
            // first call this function to setup all the parameter bindings
            ParameterBinding[] bindings = commandOptions.SetupCommandParameterBindings();

            // Add command, at this point the action should already be set?
            this.RootCommand.Subcommands.Add(commandOptions.Command);
            
            // add options to a dictionary by the command name
            cmdBindingFactory.AddCommandOptions(commandOptions.Command.Name, commandOptions);
        }

        if (this.RootCommand is null)
            throw new InvalidOperationException("RootCommand should not be null here");

        // build host
        IHost host = this.builder.Build();

        // setup root command here
        if (rootOptions is not null)
        {
            var rootAction = rootOptions.Handler(host.Services);
            this.RootCommand.SetAction(rootAction);
        }

        MinimalCommandLineApp app = new(
            host,
            this.cmdExecutionMode,
            this.RootCommand,
            this.Configuration,
            this.args
        );

        return app;
    }

    /// <summary>
    /// Use shell mode as the default mode for execution.
    /// <para>The default is CLI Mode but shell mode is allowed.</para>
    /// </summary>
    public MinimalCommandLineBuilder UseShellMode()
    {
        this.cmdExecutionMode = CommandExecutionMode.ShellDefault;
        return this;
    }

    /// <summary>
    /// Disable CLI mode for execution, this requires shell mode for the user.
    /// <para>The default is CLI Mode but shell mode is allowed.</para>
    /// </summary>
    public MinimalCommandLineBuilder RequireShellMode()
    {
        this.cmdExecutionMode = CommandExecutionMode.ShellRequired;
        return this;
    }

    /// <summary>
    /// Disable shell mode for execution, this requires CLI mode for the user.
    /// <para>The default is CLI Mode but shell mode is allowed.</para>
    /// </summary>
    public MinimalCommandLineBuilder RequireCliMode()
    {
        this.cmdExecutionMode = CommandExecutionMode.CliRequired;
        return this;
    }

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) 
        where TContainerBuilder : notnull
    {
        throw new NotImplementedException();
    }
}
