using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

public class MinimalCommandLineApp : IHost
{
    public MinimalCommandLineApp(Command rootCommand, IServiceProvider services, IConfigurationRoot configuration)
    {
        // pass in the root command after it is built from DI
        RootCommand = rootCommand;

        Services = services;
        Configuration = configuration;
    }

    public IServiceProvider Services { get; private set; }
    public IConfigurationRoot Configuration { get; private set; }

    internal Command RootCommand { get; private set; }

    public void SetRootHandler(Delegate handler)
    {
        var parameters = handler.Method.GetParameters();

        // ensure the count of command arguments/options matches the count of parameters
        var symbolCount = RootCommand.Arguments.Count + RootCommand.Options.Count;
        if (parameters.Length > symbolCount)
        {
            var missingParameter = parameters[symbolCount];

            throw new ArgumentException(nameof(handler),
                $"The number of Handler parameters for command {RootCommand.Name} " +
                $"is greater than the provided arguments and options, could not find " +
                $"an Argument or Option for {missingParameter.Name}.");
        }
        if (symbolCount > parameters.Length)
            throw new ArgumentException(nameof(handler),
                $"The number of arguments and options for command {RootCommand.Name} " +
                $"is greater than the parameters of the handler.");

        var symbols = new List<(Type ValueType, Symbol symbol)>();

        // go through each argument and option in order, and compare them with each parameter
        for (int i = 0; i < RootCommand.Arguments.Count + RootCommand.Options.Count; i++)
        {
            var parameter = parameters[i];
            var paramIsOptional = parameter.IsOptional;

            if (i < RootCommand.Arguments.Count)
            {
                var argument = RootCommand.Arguments[i];

                if (argument.ValueType != parameter.ParameterType)
                    throw new Exception($"Argument ({argument.Name}) and parameter ({parameter.Name}) type mismatch.");

                // by convention, if the parameter is optional, grab the default value and add it to the documentation
                if (paramIsOptional && !argument.HasDefaultValue)
                    argument.SetDefaultValue(parameter.DefaultValue);
            }
            else
            {
                var option = RootCommand.Options[i - RootCommand.Arguments.Count];

                if (paramIsOptional)
                    option.SetDefaultValue(parameter.DefaultValue);

                // by convention, if the parameter is required and the option is not, set the option to be required
                if (!paramIsOptional && !option.IsRequired)
                    option.IsRequired = true;

                if (option.ValueType != parameter.ParameterType)
                    throw new Exception($"Option ({option.Name}) and parameter ({parameter.Name}) type mismatch.");

                // if the parameter is optional, grab the default value and add it to the documentation
                if (paramIsOptional && option.IsRequired)
                    throw new Exception($"Optional Option ({option.Name}) and required parameter mismatch.");
            }
        }

        _delegateHandler = handler;

        RootCommand.SetHandler(delegateCaller);
    }
    Delegate? _delegateHandler;
    void delegateCaller(InvocationContext context)
    {
        if (_delegateHandler is null)
            throw new ArgumentNullException("Handler",
                $"Delegating handler for command \"{RootCommand.Name}\" was not set.");

        var dynamicArguments = new List<object?>();

        foreach (var arg in RootCommand.Arguments)
        {
            var argVal = context.ParseResult.GetValueForArgument(arg);
            dynamicArguments.Add(argVal);
        }
        foreach (var opt in RootCommand.Options)
        {
            var argVal = context.ParseResult.GetValueForOption(opt);
            if(opt.Name != "version" 
                && !opt.HasAlias("--version")
                && opt.Name != "help")
            {
                dynamicArguments.Add(argVal);
            }
        }

        // run the method based on the return type
        var returnType = _delegateHandler.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            var task = (Task)_delegateHandler.DynamicInvoke(dynamicArguments.ToArray());

            task.ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        else if (returnType == typeof(void))
        {
            var result = _delegateHandler.DynamicInvoke(dynamicArguments.ToArray());
        }
        else
        {
            throw new NotSupportedException($"A handler of type {returnType} is not supported.");
        }
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
    public MinimalCommandLineApp AddRootArgument<T>(string name, Action<ArgumentBuilder<T>>? argOptions = null)
    {
        var arg = new Argument<T>(name);

        if (argOptions is not null)
        {
            var argBuilder = new ArgumentBuilder<T>(arg);
            argOptions(argBuilder);
        }

        RootCommand.AddArgument(arg);
        return this;
    }
    public MinimalCommandLineApp AddRootOption<T>(string name, Action<OptionBuilder<T>>? options = null)
    {
        var opt = new Option<T>(name);

        if (options is not null)
        {
            var optBuilder = new OptionBuilder<T>(opt);
            options(optBuilder);
        }

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

    public void Invoke(string[] args)
    {
        RootCommand.Invoke(args);
    }
    public async Task InvokeAsync(string[] args)
    {
        await RootCommand.InvokeAsync(args);
    }

    Task IHost.StartAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();
    Task IHost.StopAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();
    void IDisposable.Dispose()
        => throw new NotImplementedException();
}
