using System.CommandLine.Minimal;

var app = new MinimalCommandLineBuilder()
    .Build();

app.AddRootDescription("A simple demo app for the command line.")
    .AddRootArgument<string>("Message")
    .AddRootOption<string>("--first-option", opt => opt.AddAlias("-o1"))
    .AddRootOption<string>("--second-option")
    .SetRootHandler(
        (string message, string option1, string option2) =>
        {
            Console.WriteLine($"Hello World!  {message}");
            Console.WriteLine($"  Option 1:{option1}, Option2 {option2}");
        }
    );

app.Execute(args);