using System.Threading.Tasks;

namespace MinimalCli;

internal interface ICommandExecutor
{
    int Execute(RootCommand rootCommand, string[] args);
    Task<int> ExecuteAsync(RootCommand rootCommand, string[] args);
}
