using System.CommandLine;

namespace HelloWorld;

public class Root : RootCommand
{
    public Root()
    {
        Description = "Echo a message on the command line.";

        var arg = new Argument<string?>();
        arg.AddCompletions("Hello!", "Hello World!", "Hi!");
        arg.Description = "The message you want to echo.";
        arg.SetDefaultValue(null);

        this.AddArgument(arg);

        this.SetHandler(Execute, arg);
    }

    public void Execute(string? message)
    {
        if(message is null)
        {
            if(Console.BackgroundColor == ConsoleColor.Black)
                Console.ForegroundColor = ConsoleColor.DarkYellow;

            Console.Error.WriteLine("Please provide a message.");

            Console.ResetColor();
            return;
        }

        Console.WriteLine(message);
    }
}
