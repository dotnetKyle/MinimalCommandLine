using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

public class CommandBuilder<THandler>
{
    // implement a new version of commandbuilder that knows what type of handler it has
}
public class CommandBuilder
{
    IServiceProvider _serviceProvider;
    internal Command Command;

    internal CommandBuilder(Command cmd, IServiceProvider serviceProvider)
    {
        Command = cmd;
        _serviceProvider = serviceProvider;
    }


    public CommandBuilder AddCommandDescription(string description)
    {
        Command.Description = description;

        return this;
    }
    public CommandBuilder AddAlias(string alias)
    {
        Command.AddAlias(alias);
        return this;
    }
    public CommandBuilder AddArgument<T>(string name, Action<ArgumentBuilder<T>>? argOptions = null)
    {
        var arg = new Argument<T>(name);
        
        if(argOptions is not null)
        {
            var opt = new ArgumentBuilder<T>(arg);
            argOptions(opt);
        }

        Command.AddArgument(arg);

        return this;
    }
    public CommandBuilder AddOption<T>(string name, Action<OptionBuilder<T>>? options = null)
    {
        var option = new Option<T>(name);

        if(options is not null)
        {
            var optBuilder = new OptionBuilder<T>(option);
            options(optBuilder);
        }

        Command.AddOption(option);

        return this;
    }
    public CommandBuilder MapHandler<THandler>(Func<THandler, Delegate> mapper)
        where THandler : class
    {
        // use DI during runtime to get an instance of the handler
        HandlerType = typeof(THandler);
        DelegateLocator = (Func<object, Delegate>)mapper;
        Command.SetHandler(handlerActivator);
        return this;
    }
    public CommandBuilder SetHandler(Delegate handler)
    {
        var parameters = handler.Method.GetParameters();

        // ensure the count of command arguments/options matches the count of parameters
        var symbolCount = Command.Arguments.Count + Command.Options.Count;
        if (parameters.Length > symbolCount)
        {
            var missingParameter = parameters[symbolCount];

            throw new ArgumentException(nameof(handler),
                $"The number of Handler parameters for command {Command.Name} " +
                $"is greater than the provided arguments and options, could not find " +
                $"an Argument or Option for {missingParameter.Name}.");
        }
        if (symbolCount > parameters.Length)
            throw new ArgumentException(nameof(handler),
                $"The number of arguments and options for command {Command.Name} " +
                $"is greater than the parameters of the handler.");

        var symbols = new List<(Type ValueType, Symbol symbol)>();

        // go through each argument and option in order, and compare them with each parameter
        for (int i = 0; i < Command.Arguments.Count + Command.Options.Count; i++)
        {
            var parameter = parameters[i];
            var paramIsOptional = parameter.IsOptional;

            if(i < Command.Arguments.Count)
            {
                var argument = Command.Arguments[i];

                if (argument.ValueType != parameter.ParameterType)
                    throw new Exception($"Argument ({argument.Name}) and parameter ({parameter.Name}) type mismatch.");

                // by convention, if the parameter is optional, grab the default value and add it to the documentation
                if (paramIsOptional && !argument.HasDefaultValue)
                    argument.SetDefaultValue(parameter.DefaultValue);
            }
            else
            {
                var option = Command.Options[i - Command.Arguments.Count];

                if(paramIsOptional)
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

        Command.SetHandler(delegateCaller);

        return this;
    }

    Type? HandlerType;
    Func<object, Delegate> DelegateLocator;
    Delegate? _delegateHandler;
    void handlerActivator(InvocationContext context)
    {
        var handler = _serviceProvider.GetRequiredService(HandlerType);
        var dlgt = DelegateLocator(handler);

        if (dlgt is null)
            throw new ArgumentNullException("Handler",
                $"Delegating handler for command \"{Command.Name}\" was not set.");

        var dynamicArguments = new List<object?>();

        foreach (var arg in Command.Arguments)
        {
            var argVal = context.ParseResult.GetValueForArgument(arg);
            dynamicArguments.Add(argVal);
        }
        foreach (var opt in Command.Options)
        {
            var argVal = context.ParseResult.GetValueForOption(opt);
            dynamicArguments.Add(argVal);
        }

        // run the method based on the return type
        var returnType = dlgt.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            var task = (Task)dlgt.DynamicInvoke(dynamicArguments.ToArray());

            task.ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        else if (returnType == typeof(void))
        {
            var result = dlgt.DynamicInvoke(dynamicArguments.ToArray());
        }
        else
        {
            throw new NotSupportedException($"A handler of type {returnType} is not supported.");
        }
    }
    void delegateCaller(InvocationContext context)
    {
        if (_delegateHandler is null)
            throw new ArgumentNullException("Handler",
                $"Delegating handler for command \"{Command.Name}\" was not set.");

        var dynamicArguments = new List<object?>();

        foreach (var arg in Command.Arguments)
        {
            var argVal = context.ParseResult.GetValueForArgument(arg);
            dynamicArguments.Add(argVal);
        }
        foreach (var opt in Command.Options)
        {
            var argVal = context.ParseResult.GetValueForOption(opt);
            dynamicArguments.Add(argVal);
        }

        // run the method based on the return type
        var returnType = _delegateHandler.Method.ReturnType;

        if(returnType == typeof(Task))
        {
            var task = (Task)_delegateHandler.DynamicInvoke(dynamicArguments.ToArray());

            task.ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        else if(returnType == typeof(void))
        {
            var result = _delegateHandler.DynamicInvoke(dynamicArguments.ToArray());
        }
        else
        {
            throw new NotSupportedException($"A handler of type {returnType} is not supported.");
        }
    }
}