using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;
public class MinimalCommandLineApp : IHostedService
{
    private readonly string[] args;
    private readonly CommandExecutionMode cmdExecutionMode;
    private CommandExecutorCli CliCommandExecutor => this.Services.GetRequiredService<CommandExecutorCli>();
    private CommandExecutorShell ShellCommandExecutor => this.Services.GetRequiredService<CommandExecutorShell>();

    internal MinimalCommandLineApp(MinimalCommandLineBuilder builder, string[] args)
    {
        this.Host = builder.builder.Build();
        this.cmdExecutionMode = builder.cmdExecutionMode;
        this.Configuration = builder.Configuration;
        this.args = args;
        this.RootCommand = new();
    }

    internal readonly Dictionary<string, Func<ParseResult, object?>> ArgumentParsers = new();
    internal readonly Dictionary<string, Func<ParseResult, object?>> OptionParsers = new();

    public IHost Host { get; private set; }
    public IServiceProvider Services => this.Host.Services;
    public IConfiguration Configuration { get; private set; }

    internal RootCommand RootCommand { get; private set; }

    /// <summary>
    /// Start the application.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (this.cmdExecutionMode == CommandExecutionMode.CliRequired)
        {
            await this.CliCommandExecutor!.ExecuteAsync(this.RootCommand, this.args);
        }
        else if (this.cmdExecutionMode == CommandExecutionMode.ShellRequired)
        {
            await this.ShellCommandExecutor!.ExecuteAsync(this.RootCommand, this.args);
        }
        else if(this.cmdExecutionMode == CommandExecutionMode.ShellDefault)
        {
            if (this.args.Contains("--non-interactive"))
                await this.CliCommandExecutor!.ExecuteAsync(this.RootCommand, this.args);
            else
                await this.ShellCommandExecutor!.ExecuteAsync(this.RootCommand, this.args);
        }
        else if(this.cmdExecutionMode == CommandExecutionMode.CliDefault)
        {
            if(this.args.Contains("--shell"))
                await this.CliCommandExecutor!.ExecuteAsync(this.RootCommand, this.args);
            else
                await this.ShellCommandExecutor!.ExecuteAsync(this.RootCommand, this.args);
        }
    }
    public Task StartAsync() => this.StartAsync(CancellationToken.None);
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
                List<Option> validOptions = new();
                foreach(var o in RootCommand.Options)
                {
                    if (o.Name == "--help" || o.Name == "--version")
                        continue;

                    validOptions.Add(o);
                }
                var option = validOptions[i - RootCommand.Arguments.Count];

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
                object? argValue = this.ArgumentParsers[arg.Name]
                    .Invoke(parseResult);
                dynamicArguments.Add(argValue);
            }
            foreach (Option opt in RootCommand.Options)
            {
                if (opt.Name == "--help" || opt.Name == "--version")
                    continue;

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

    public MinimalCommandLineApp AddPrompt(string prompt)
    {
        this.Services.GetRequiredService<CommandExecutorShell>().SetPrompt(prompt);
        return this;
    }

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
        this.ArgumentParsers.Add(name, (ParseResult result) => result.GetValue<T>(name));

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
        this.OptionParsers.Add(name, (ParseResult result) => result.GetValue<T>(name));

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
