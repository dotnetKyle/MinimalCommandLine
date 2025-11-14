using MinimalCli;

namespace MinimalCliApp.Commands;

internal class GreetCommand
{
    // this attribute creates the main command handler
    [RootHandler]
    public void Execute(string? yourName = null)
    {
        if(!string.IsNullOrEmpty(yourName))
        {
            Console.WriteLine("Hello {0}!", yourName);
            return;
        }
        else
        {
            Console.WriteLine("Hello World!");
        }
    }
}
