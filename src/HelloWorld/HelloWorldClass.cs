using System.CommandLine;
using System.CommandLine.Minimal;

namespace Hello;

public class HelloWorldClass
{
    [Command("hi")]
    public void Execute(string message, [Option] string option1, [Option] string option2)
    {
        Console.WriteLine($"Hello World!  Message:\"{message}\"");
        Console.WriteLine($"  Option 1:\"{option1}\" value.");
        Console.WriteLine($"  Option 2:\"{option2}\" value.");
    }
}
