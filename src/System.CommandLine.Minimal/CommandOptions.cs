using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

public class CommandOptions
{
    internal CommandOptions(Command cmd)
    {
        Command = cmd;
    }

    internal Command Command;

    public CommandOptions AddCommandDescription(string description)
    {
        Command.Description = description;

        return this;
    }
    public CommandOptions AddAlias(string alias)
    {
        Command.AddAlias(alias);
        return this;
    }
    public CommandOptions AddArgument<T>(string name, string? description = null)
    {
        var arg = new Argument<T>(name, description);
        Command.AddArgument(arg);
        return this;
    }
    public CommandOptions AddOption<T>(string name, string? description = null)
    {
        var opt = new Option<T>(name, description);
        Command.AddOption(opt);
        return this;
    }
    public CommandOptions SetHandler(Delegate handler)
    {
        var parameters = handler.Method.GetParameters();

        var symbolTypes = Command.Arguments
            .Select(arg => arg.ValueType)
            .ToList();
        symbolTypes.AddRange(
            Command.Options.Select(opt => opt.ValueType)
        );

        if (parameters.Length > symbolTypes.Count)
            throw new ArgumentException(nameof(handler),
                $"The number of Handler parameters for command {Command.Name} " +
                $"is greater than the provided arguments and options.");

        if (symbolTypes.Count > parameters.Length)
            throw new ArgumentException(nameof(handler),
                $"The number of arguments and options for command {Command.Name} " +
                $"is greater than the parameters of the handler.");

        for (int i = 0; i < symbolTypes.Count; i++)
        {
            var symbol = symbolTypes[i];
            var parameter = parameters[i];
            if (symbol != parameter.ParameterType)
                throw new ArgumentException(nameof(handler), 
                    $"Handler parameters Types for command {Command.Name} " +
                    $"do not match the provided arguments and options.  " +
                    $"Parameter[{i}] is {parameter.ParameterType.Name} and handler[{i}] is {symbol.Name}.");
        }

        _delegateHandler = handler;
        Command.SetHandler(delegateCaller);
        return this;
    }

    Delegate? _delegateHandler;

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

        var reault = _delegateHandler.DynamicInvoke(dynamicArguments.ToArray());

    }
    Task handlerAsync(InvocationContext context)
    {
        throw new NotImplementedException();
    }
}