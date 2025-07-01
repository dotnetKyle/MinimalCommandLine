using System.Threading.Tasks;

namespace System.CommandLine.Minimal;

internal interface ICommandExecutor
{
    int Execute(RootCommand rootCommand, string[] args);
    Task<int> ExecuteAsync(RootCommand rootCommand, string[] args);
}
