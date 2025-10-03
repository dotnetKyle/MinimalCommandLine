using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Minimal;
using System.Runtime.CompilerServices;

MinimalCommandLineBuilder builder = new(args);
builder.Services.TryAddTransient<HiCommandOptions>();
builder.MapAllCommands();
HiCommandOptions h;
MinimalCommandLineApp app = builder.Build();

static void Get(ParseResult parseResult)
{
    Argument<string> myArg = new("");
    string? val = parseResult.GetValue(myArg);

}

app.AddRootDescription("A simple demo app for the command line.")
    //.AddRootArgument<string>("Message")
    //.AddRootOption<string>("--first-option", opt => opt.AddAlias("-o1"))
    //.AddRootOption<string>("--second-option")
    //.SetRootHandler(
    //    [Command("helloworld")]
    //    static (string message, string option1, string option2) =>
    //    {
    //        Console.WriteLine($"Hello World!  {message}");
    //        Console.WriteLine($"  Option 1:{option1}, Option2 {option2}");
    //    }
    //)
    ;

await app.StartAsync();
