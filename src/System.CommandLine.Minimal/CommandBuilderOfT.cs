using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.CommandLine.Invocation;
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

    public CommandBuilder<THandler> AddCommandDescription(string description)
    {
        Command.Description = description;

        return this;
    }
    public CommandBuilder<THandler> AddAlias(string alias)
    {
        Command.AddAlias(alias);
        return this;
    }
    public CommandBuilder<THandler> AddArgument<T>(string name, Action<ArgumentBuilder<T>>? argOptions = null)
    {
        var arg = new Argument<T>(name);

        if (argOptions is not null)
        {
            var opt = new ArgumentBuilder<T>(arg);
            argOptions(opt);
        }

        Command.AddArgument(arg);

        return this;
    }
    public CommandBuilder<THandler> AddOption<T>(string name, Action<OptionBuilder<T>>? options = null)
    {
        var option = new Option<T>(name);

        if (options is not null)
        {
            var optBuilder = new OptionBuilder<T>(option);
            options(optBuilder);
        }

        Command.AddOption(option);

        return this;
    }

    internal Func<THandler, Delegate> DelegateLocator;
    internal void handlerActivator(InvocationContext context)
    {
        var handler = _serviceProvider.GetRequiredService<THandler>();
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
}
