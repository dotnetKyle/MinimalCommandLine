using System.Collections.Generic;

namespace System.CommandLine.Minimal;

/// <summary>
/// The context of a <see cref="Command"/> being added and configured during the Build step.
/// </summary>
/// <typeparam name="TContext">The type of builder in this context</typeparam>
public interface ICommandContext<TContext> where TContext : notnull
{
    TContext Configure(Action<Command> command);
    TContext AddCommand<TCommand>()
        where TCommand : Command;
    TContext AddCommand<TCommand>(CommandContext context)
        where TCommand : Command;
}

/// <inheritdoc cref="ICommandContext{TContext}"/>
public class CommandContext : ICommandContext<CommandContext>
{
    /// <summary>
    /// Create the default implementation of <see cref="ICommandContext{TContext}"/>
    /// </summary>
    public CommandContext()
    {
        SubCommands = new Dictionary<Type, CommandContext>();
        CommandConfiguration = (cmd) => { };
    }

    internal Dictionary<Type, CommandContext> SubCommands;
    internal Action<Command> CommandConfiguration;

    /// <summary>
    /// Configure this <see cref="Command"/>.
    /// </summary>
    /// <param name="configure">The action to configure the <see cref="Command"/> after activation during build.</param>
    /// <returns>The context for chaining</returns>
    public CommandContext Configure(Action<Command> configure)
    {
        CommandConfiguration = configure;
        return this;
    }

    /// <summary>
    /// Add a Command with some custom configuration.
    /// </summary>
    /// <typeparam name="TCommand">The <see cref="Command"/> to add.</typeparam>
    /// <returns>The context for chaining</returns>
    /// <exception cref="NotImplementedException"></exception>
    public CommandContext AddCommand<TCommand>() 
        where TCommand : Command
    {
        SubCommands.Add(typeof(TCommand), new CommandContext());
        return this;
    }

    /// <summary>
    /// Add a Command with some custom configuration.
    /// </summary>
    /// <typeparam name="TCommand">The <see cref="Command"/> to add.</typeparam>
    /// <param name="configure">The action to configure the <see cref="Command"/>'s sub-commands after activation during build.</param>
    /// <returns>The context for chaining</returns>
    /// <exception cref="NotImplementedException"></exception>
    public CommandContext AddCommand<TCommand>(CommandContext configure) 
        where TCommand : Command
    {
        SubCommands.Add(typeof(TCommand), configure);
        return this;
    }

}
