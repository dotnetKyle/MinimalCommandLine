using MinimalCli;

namespace MinimalCliApp.Commands;

internal class EchoCommand
{
    // creates a sub-command that can be called by using the 'echo-command' sub-command
    [Handler("echo-command")]
    public Task ExecuteAsync(string prompt)
    {
        Console.WriteLine("Echo: {0}", prompt);

        return Task.CompletedTask;
    }
}
