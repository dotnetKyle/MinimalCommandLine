using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace System.CommandLine.Minimal;
public class MinimalCommandLineBuilder : IHostBuilder, ICommandContext<MinimalCommandLineBuilder>
{
    static readonly string assemblyName;
    static readonly string assemblyDirectory;

    Dictionary<Type, CommandContext> subCommands;
    Action<Command> commandConfiguration;

    Type rootCommandType;

    // static constructor
    static MinimalCommandLineBuilder()
    {
        var assembly = Assembly.GetEntryAssembly();
        assemblyName = assembly.GetName().Name;
        assemblyDirectory = Path.GetDirectoryName(assembly.Location);
    }

    /// <summary>
    /// Create the default Minimal Command Line Builder.
    /// </summary>
    public MinimalCommandLineBuilder()
    {
        rootCommandType = typeof(RootCommand);
        subCommands = new Dictionary<Type, CommandContext>();
        commandConfiguration = (c) => { };

        Properties = new Dictionary<object, object>();
        // add any default properties here:


        Services = new ServiceCollection();
        // add any default services here:


        ConfigurationManager = new ConfigurationManager();
        // add any default configuration here:
        ConfigurationManager.AddEnvironmentVariables("MINIMAL_CLI_");
        ConfigurationManager.AddJsonFile("appsettings.json", optional:true);

    }

    /// <summary>
    /// The <see cref="IServiceCollection"/> for dependency injection
    /// </summary>
    public ServiceCollection Services { get; private set; }

    /// <summary>
    /// The <see cref="IConfigurationBuilder"/> and the <see cref="IConfiguration"/>.
    /// </summary>
    public ConfigurationManager ConfigurationManager { get; private set; }

    /// <summary>
    /// Properties shared during the build step.
    /// </summary>
    public IDictionary<object, object> Properties { get; private set; }

    /// <summary>
    /// Build the CLI
    /// </summary>
    /// <returns></returns>
    public MinimalCommandLineApp Build()
    {
        var configRoot = ((IConfigurationBuilder)ConfigurationManager).Build();

        Services.AddSingleton<IConfiguration>(configRoot);

        var services = Services.BuildServiceProvider();

        // add any default SubCommands here (they will be appended to the end of the root):


        // first, create the root command:
        var rootInstance = activateCommandInstance(services, rootCommandType);
        commandConfiguration(rootInstance);

        // then, recursive build all of the child commands
        recursivelyBuildCommands(services, rootInstance, subCommands);

        // TODO: pass rootInstance into app
        return new MinimalCommandLineApp(rootInstance, services, configRoot);
    } 
    IHost IHostBuilder.Build()
        => Build();

    Command activateCommandInstance(IServiceProvider services, Type cmdType)
    {
        // determine what parameters are need by using the first constructor

        var ctor = cmdType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault()
            ?? throw new InvalidOperationException($"No public instance constructor can be found for the Command of Type: {cmdType.FullName}.");

        var p = new List<object>();
        foreach( var c in ctor.GetParameters())
        {
            var svc = services.GetRequiredService(c.ParameterType);
            p.Add(svc);
        }


        var instance = Activator.CreateInstance(cmdType, p.ToArray()) as Command;

        if (instance is null)
            throw new InvalidOperationException($"Could not activate the Command: {cmdType.FullName} during {nameof(Build)}");

        return instance;
    }
    void recursivelyBuildCommands(IServiceProvider services, Command rootCmd, Dictionary<Type, CommandContext> subCommands)
    {
        // when the sub-commands list is empty, the tree branch has reaches it's end
        foreach(var kvp in subCommands)
        {
            var subCommandType = kvp.Key;
            var subCommandContext = kvp.Value;

            var subCommandInstance = activateCommandInstance(services, subCommandType);
            subCommandContext.CommandConfiguration(subCommandInstance);

            rootCmd.AddCommand(subCommandInstance);

            // continue building tree
            recursivelyBuildCommands(services, subCommandInstance, subCommandContext.SubCommands);
        }
    }

    /// <summary>
    /// Create a new instance of the <see cref="HostBuilderContext"/>
    /// </summary>
    /// <returns></returns>
    HostBuilderContext getHostBuilderContext()
    {
        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Production;

        var hostEnvironment = MinimalCommandLineHostEnvironment.CreateInstance(
            environmentName: dotnetEnvironment,
            applicationName: assemblyName,
            contentRootPath: assemblyDirectory
        );

        return new HostBuilderContext(Properties)
        {
            Configuration = ConfigurationManager,
            HostingEnvironment = hostEnvironment
        };
    }

    public MinimalCommandLineBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        configureDelegate(getHostBuilderContext(), ConfigurationManager);
        return this;
    }
    public MinimalCommandLineBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        configureDelegate(ConfigurationManager);
        return this;
    }
    public MinimalCommandLineBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        configureDelegate(getHostBuilderContext(), Services);
        return this;
    }
    public MinimalCommandLineBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
    {
        configureDelegate(Services);
        return this;
    }

    IHostBuilder IHostBuilder.ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        => ConfigureAppConfiguration(configureDelegate);
    IHostBuilder IHostBuilder.ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        => ConfigureHostConfiguration(configureDelegate);
    IHostBuilder IHostBuilder.ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        => ConfigureServices(configureDelegate);

    // Not yet implemented
    IHostBuilder IHostBuilder.ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        => throw new NotImplementedException();
    IHostBuilder IHostBuilder.UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        => throw new NotImplementedException();
    IHostBuilder IHostBuilder.UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        => throw new NotImplementedException();

    /// <summary>
    /// Override the default root command type for the entire tool
    /// </summary>
    /// <typeparam name="TRootCommand">The type of <see cref="RootCommand"/> to use for this CLI.</typeparam>
    /// <returns>The builder for chaining</returns>
    public MinimalCommandLineBuilder OverrideRootCommand<TRootCommand>() where TRootCommand : RootCommand
    {
        rootCommandType = typeof(TRootCommand);
        return this;
    }

    /// <summary>
    /// Configure the <see cref="RootCommand"/> for this CLI.
    /// </summary>
    /// <param name="configure">Configure the <see cref="RootCommand"/></param>
    /// <returns>The builder for chaining</returns>
    public MinimalCommandLineBuilder ConfigureRoot(Action<Command> configure)
    {
        commandConfiguration = configure;
        return this;
    }
    MinimalCommandLineBuilder ICommandContext<MinimalCommandLineBuilder>.Configure(Action<Command> command)
        => ConfigureRoot(command);

    /// <summary>
    /// Adds a <see cref="Command"/> to the root of the CLI
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns>The builder for chaining</returns>
    public MinimalCommandLineBuilder AddCommand<TCommand>() where TCommand : Command
    {
        subCommands.Add(typeof(TCommand), new CommandContext());
        return this;
    }

    /// <summary>
    /// Adds a <see cref="Command"/> to the root of the CLI with some additional configuration
    /// </summary>
    /// <typeparam name="TCommand">The type of command to add</typeparam>
    /// <param name="configure">Configure this <see cref="Command"/> by adding Sub-Commands and more.</param>
    /// <returns>The builder for chaining</returns>
    public MinimalCommandLineBuilder AddCommand<TCommand>(CommandContext configure) where TCommand : Command
    {
        subCommands.Add(typeof(TCommand), configure);
        return this;
    }
}
