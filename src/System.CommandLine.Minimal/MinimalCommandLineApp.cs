using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.CommandLine.Parsing;
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

    public async Task<int> ExecuteAsync(string[] args)
    {
        var parseResult = RootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }
    
    public int Execute(string[] args)
    {
        var parseResult = RootCommand.Parse(args);
        return parseResult.Invoke();
    }

    public void SetRootHandler(Delegate handler)
    {
        var parameters = handler.Method.GetParameters();

        // ensure the count of command arguments/options matches the count of parameters
        int symbolCount = RootCommand.Arguments.Count;
        foreach (var option in RootCommand.Options)
        {
            if (option.Name == "--help" || option.Name == "--version")
                continue;
            symbolCount++;
        }

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
        for (int i = 0; i < symbolCount; i++)
        {
            var parameter = parameters[i];
            var paramIsOptional = parameter.IsOptional;

            if (i < RootCommand.Arguments.Count)
            {
                var argument = RootCommand.Arguments[i];

                if (argument.ValueType != parameter.ParameterType)
                    throw new Exception($"Argument ({argument.Name}) and parameter ({parameter.Name}) type mismatch.");

                // by convention, if the parameter is optional and has a default value
                if (paramIsOptional && argument.GetType().IsGenericType)
                {
                    var defaultValue = parameter.DefaultValue;
                    if (defaultValue != null)
                    {
                        var method = argument.GetType().GetMethod("SetDefaultValue");
                        if (method != null)
                        {
                            method.Invoke(argument, new[] { defaultValue });
                        }
                    }
                }
            }
            else
            {
                var option = RootCommand.Options[i - RootCommand.Arguments.Count];

                if (paramIsOptional && option.GetType().IsGenericType)
                {
                    var defaultValue = parameter.DefaultValue;
                    if (defaultValue != null)
                    {
                        var method = option.GetType().GetMethod("SetDefaultValue");
                        if (method != null)
                        {
                            method.Invoke(option, new[] { defaultValue });
                        }
                    }
                }

                // by convention, if the parameter is required and the option is not, set the option to be required
                if (!paramIsOptional)
                {
                    option.Required = true;
                }

                if (option.ValueType != parameter.ParameterType)
                    throw new Exception($"Option ({option.Name}) and parameter ({parameter.Name}) type mismatch.");

                // if the parameter is optional, grab the default value and add it to the documentation
                if (paramIsOptional && option.Required)
                    throw new Exception($"Optional Option ({option.Name}) and required parameter mismatch.");
            }
        }

        _delegateHandler = handler;

        RootCommand.SetAction((parseResult) =>
        {
            var dynamicArguments = new List<object?>();

            foreach (Argument arg in RootCommand.Arguments)
            {
                ArgumentResult? argVal = parseResult.GetResult(arg);
                dynamicArguments.Add(argVal);
            }
            foreach (Option opt in RootCommand.Options)
            {
                OptionResult? optVal = parseResult.GetResult(opt);
                dynamicArguments.Add(optVal);
            }

            // run the method based on the return type
            var returnType = _delegateHandler.Method.ReturnType;

            if (returnType == typeof(Task))
            {
                var task = (Task)_delegateHandler.DynamicInvoke(dynamicArguments.ToArray());
                return task;
            }
            else if (returnType == typeof(void))
            {
                _delegateHandler.DynamicInvoke(dynamicArguments.ToArray());
                return Task.CompletedTask;
            }
            else
            {
                throw new NotSupportedException($"A handler of type {returnType} is not supported.");
            }
        });
    }

    private Delegate? _delegateHandler;

    public MinimalCommandLineApp AddRootDescription(string desc)
    {
        RootCommand.Description = desc;
        return this;
    }

    public MinimalCommandLineApp AddRootAlias(string alias)
    {
        RootCommand.Aliases.Add(alias);
        return this;
    }

    public MinimalCommandLineApp AddRootArgument<T>(string name, Action<ArgumentBuilder<T>>? argOptions = null)
    {
        var arg = new Argument<T>(name);

        if (argOptions is not null)
        {
            var argBuilder = new ArgumentBuilder<T>(name, arg);
            argOptions(argBuilder);
        }

        RootCommand.Add(arg);
        return this;
    }

    public MinimalCommandLineApp AddRootOption<T>(string name, Action<OptionBuilder<T>>? options = null)
    {
        var opt = new Option<T>(name);

        if (options is not null)
        {
            var optBuilder = new OptionBuilder<T>(name, opt);
            options(optBuilder);
        }

        RootCommand.Add(opt);
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

        cmd.SetAction(builder.handlerActivator);
        
        RootCommand.Add(cmd);

        return this;
    }

    public MinimalCommandLineApp AddCommand(string commandName, Action<CommandBuilder> cmdOptions)
    {
        var cmd = new Command(commandName);
        var opt = new CommandBuilder(cmd);
        cmdOptions(opt);
        RootCommand.Add(cmd);
        return this;
    }
}
