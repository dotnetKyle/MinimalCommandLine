using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinimalCli;

public class CommandBuilder
{
    internal Command Command;

    internal CommandBuilder(Command cmd)
    {
        Command = cmd;
    }

    internal readonly Dictionary<string, Func<ParseResult, object?>> ArgumentParsers = new();
    internal readonly Dictionary<string, Func<ParseResult, object?>> OptionParsers = new();

    public CommandBuilder AddCommandDescription(string description)
    {
        Command.Description = description;
        return this;
    }

    public CommandBuilder AddAlias(string alias)
    {
        Command.Aliases.Add(alias);
        return this;
    }

    public CommandBuilder AddArgument<T>(string name, Action<ArgumentBuilder<T>>? argOptions = null)
    {
        var arg = new Argument<T>(name);
        this.ArgumentParsers.Add(name, (ParseResult result) => result.GetValue<T>(name));
        
        if(argOptions is not null)
        {
            var argBuilder = new ArgumentBuilder<T>(name, arg);
            argOptions(argBuilder);
        }

        Command.Add(arg);
        return this;
    }

    public CommandBuilder AddOption<T>(string name, Action<OptionBuilder<T>>? options = null)
    {
        var option = new Option<T>(name);
        this.OptionParsers.Add(name, (ParseResult result) => result.GetValue<T>(name));

        if(options is not null)
        {
            var optBuilder = new OptionBuilder<T>(name, option);
            options(optBuilder);
        }

        Command.Add(option);
        return this;
    }
    
    public void SetHandler(Delegate handler)
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
                var option = Command.Options[i - Command.Arguments.Count];

                if(paramIsOptional && option.GetType().IsGenericType)
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

        Command.SetAction((parseResult) => {
            var dynamicArguments = new List<object?>();

            foreach (Argument arg in Command.Arguments)
            {
                object? argValue = this.ArgumentParsers[arg.Name]
                    .Invoke(parseResult);
                dynamicArguments.Add(argValue);
            }
            foreach (Option opt in Command.Options)
            {
                object? optionValue = this.OptionParsers[opt.Name]
                    .Invoke(parseResult);
                dynamicArguments.Add(optionValue);
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
}