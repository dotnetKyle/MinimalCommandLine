using Microsoft.Extensions.Hosting;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MinimalCli;

internal class CommandExecutorShell : ICommandExecutor
{
    private string prompt = "";
    private string[] exitCommands = [ "exit" ];
    private readonly IHostApplicationLifetime applicationLifetime;

    public CommandExecutorShell(IHostApplicationLifetime applicationLifetime)
    {
        this.applicationLifetime = applicationLifetime;
    }

    public void SetPrompt(string newPrompt)
    {
        this.prompt = newPrompt;
    }
    public void SetExitCommands(string[] exitCommands)
    {
        this.exitCommands = exitCommands;
    }

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers",
        Justification = "This is not code that runs during analysis")]
    public async Task<int> ExecuteAsync(RootCommand rootCommand, string[] args)
    {
        try
        {
            while (!this.applicationLifetime.ApplicationStopping.IsCancellationRequested)
            {
                // wait for input
                if (!GetInput(rootCommand, this.prompt, this.exitCommands, out ParseResult? result))
                {
                    this.applicationLifetime.StopApplication();
                    return 0;
                }

                if (result!.Errors.Count == 0)
                    await result.InvokeAsync(cancellationToken: this.applicationLifetime.ApplicationStopping);
                else
                    ShowParserErrors(result!);
            }
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }

        return 0;
    }
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers",
       Justification = "This is not code that runs during analysis")]
    public int Execute(RootCommand rootCommand, string[] args)
    {
        try
        {
            while (!this.applicationLifetime.ApplicationStopping.IsCancellationRequested)
            {
                // wait for input
                if (!GetInput(rootCommand, this.prompt, this.exitCommands, out ParseResult? result))
                {
                    this.applicationLifetime.StopApplication();
                    return 0;
                }

                if (result!.Errors.Count == 0)
                    result.Invoke();
                else
                    ShowParserErrors(result);
            }
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }

        return 0;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers",
        Justification = "This is not code that runs during analysis")]
    private static bool GetInput(RootCommand rootCommand, string prompt, string[] exitCommands, out ParseResult? result)
    {
        Console.Write(prompt + "> ");
        string commandString = Console.ReadLine();

        foreach (string exitCmd in exitCommands)
        {
            if (commandString.Equals(exitCmd, StringComparison.OrdinalIgnoreCase))
            {
                result = null;
                return false;
            }
        }

        result = rootCommand.Parse(commandString);
        return true;
    }

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", 
        Justification = "This is not code that runs during analysis")]
    private static void ShowParserErrors(ParseResult result)
    {
        string errorPluralized = result.Errors.Count == 1 ? "an error" : "errors";
        string errorMessage = result.CommandResult is not null
            ? $"There was {errorPluralized} running the <{result.CommandResult.Command.Name}> command:"
            : $"There was {errorPluralized} running the command:";
        Console.Error.WriteLine(errorMessage);

        foreach (ParseError error in result.Errors)
        {
            Console.Error.WriteLine(' ' + error.Message);
        }
    }
}
