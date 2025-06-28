using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

public class CommandBuilder<THandler>
    where THandler : notnull
{
    IServiceProvider _serviceProvider;
    internal Command Command;

    internal CommandBuilder(Command cmd,
        IServiceProvider serviceProvider,
        Func<THandler, Delegate> delegateLocator)
    {
        Command = cmd;
        _serviceProvider = serviceProvider;
        DelegateLocator = delegateLocator;
    }

    internal readonly Dictionary<string, Func<ParseResult, object?>> ArgumentParsers = new();
    internal readonly Dictionary<string, Func<ParseResult, object?>> OptionParsers = new();

    public CommandBuilder<THandler> AddCommandDescription(string description)
    {
        Command.Description = description;
        return this;
    }
    
    public CommandBuilder<THandler> AddAlias(string alias)
    {
        Command.Aliases.Add(alias);
        return this;
    }
    
    public CommandBuilder<THandler> AddArgument<T>(string name, Action<ArgumentBuilder<T>>? argOptions = null)
    {
        var arg = new Argument<T>(name);
        this.ArgumentParsers.Add(name, (ParseResult result) => result.GetValue<T>(name));

        if (argOptions is not null)
        {
            var argBuilder = new ArgumentBuilder<T>(name, arg);
            argOptions(argBuilder);
        }

        Command.Add(arg);
        return this;
    }
    
    public CommandBuilder<THandler> AddOption<T>(string name, Action<OptionBuilder<T>>? options = null)
    {
        var option = new Option<T>(name);
        this.OptionParsers.Add(name, (ParseResult result) => result.GetValue<T>(name));

        if (options is not null)
        {
            var optBuilder = new OptionBuilder<T>(name, option);
            options(optBuilder);
        }

        Command.Add(option);
        return this;
    }

    internal Func<THandler, Delegate> DelegateLocator;
    
    internal async Task handlerActivator(ParseResult parseResult)
    {
        THandler handler = _serviceProvider.GetRequiredService<THandler>();
        Delegate? dlgt = DelegateLocator(handler);

        if (dlgt is null)
            throw new ArgumentNullException("Handler",
                $"Delegating handler for command \"{Command.Name}\" was not set.");

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
        var returnType = dlgt.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            var task = (Task)dlgt.DynamicInvoke(dynamicArguments.ToArray());
            await task.ConfigureAwait(false);
        }
        else if (returnType == typeof(void))
        {
            dlgt.DynamicInvoke(dynamicArguments.ToArray());
        }
        else
        {
            throw new NotSupportedException($"A handler of type {returnType} is not supported.");
        }
    }
}
