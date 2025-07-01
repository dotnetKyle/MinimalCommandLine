using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal;
internal class CommandExecutorCli : ICommandExecutor
{
    private readonly IHostApplicationLifetime applicationLifetime;
    public CommandExecutorCli(IHostApplicationLifetime applicationLifetime)
    {
        this.applicationLifetime = applicationLifetime;
    }
    public async Task<int> ExecuteAsync(RootCommand rootCommand, string[] args)
    {
        ParseResult parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync(this.applicationLifetime.ApplicationStopping);
    }
    public int Execute(RootCommand rootCommand, string[] args)
    {
        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }
}
