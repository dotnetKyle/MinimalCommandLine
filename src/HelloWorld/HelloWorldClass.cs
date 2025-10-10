using System.CommandLine;
using System.CommandLine.Minimal;

namespace Hello;

public class HelloWorldClass
{
    [Command("hi")]
    public void Execute(
        string message, 
        string option1 = "opt 1 default value", 
        string option2 = "opt 2 default value")
    {
        Console.WriteLine($"Hello World!  Message:\"{message}\"");
        Console.WriteLine($"  Option 1:\"{option1}\" value.");
        Console.WriteLine($"  Option 2:\"{option2}\" value.");
    }
}
