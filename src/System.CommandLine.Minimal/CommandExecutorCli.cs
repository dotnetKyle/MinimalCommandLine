using Microsoft.Extensions.Hosting;
using System.CommandLine.Minimal.Bindings;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

internal class CommandExecutorCli : ICommandExecutor
{
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly CommandBindingFactory commandBindingFactory;
    private readonly IServiceProvider services;

    public CommandExecutorCli(
        IHostApplicationLifetime applicationLifetime, 
        CommandBindingFactory commandBindingFactory,
        IServiceProvider services)
    {
        this.applicationLifetime = applicationLifetime;
        this.commandBindingFactory = commandBindingFactory;
        this.services = services;
    }
    public async Task<int> ExecuteAsync(RootCommand rootCommand, string[] args)
    {
        ParseResult parseResult = rootCommand.Parse(args);
        CommandResult commandRes = parseResult.CommandResult;

        CommandOptions? cmdOptions = this.commandBindingFactory.TryGetOptionsFor(commandRes.Command.Name);
        if(parseResult.Action is null && cmdOptions is not null)
        {
            var handler = cmdOptions.Handler(this.services);
            int returnResult = await handler.Invoke(parseResult, this.applicationLifetime.ApplicationStopping);
            return returnResult;
        }

        // if an action is found, this is likely a help or version information call
        return await parseResult.InvokeAsync(this.applicationLifetime.ApplicationStopping);
    }
    public int Execute(RootCommand rootCommand, string[] args)
    {
        ParseResult parseResult = rootCommand.Parse(args);
        CommandResult commandRes = parseResult.CommandResult;
        string cmdName = commandRes.Command.Name;

        return parseResult.Invoke();
    }
}
