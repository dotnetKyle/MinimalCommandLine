using System.CommandLine;
using System.CommandLine.Minimal;
using DemoApp.Handlers;

var builder = new MinimalCommandLineBuilder()
    .AddRootDescription("A collection of commands for printing to the console.");

// TODO: add services up here


// TODO: var app = builder.Build();


// TODO: app.MapCommand("echo", (string message) => {
//      Console.WriteLine(message)
//  })
//   .WithDescription("Print a message to the console.")
//   .DescribeArgument<string>("Message")

builder.AddCommand("echo",
    cmdOptions => cmdOptions
    .AddCommandDescription("Print a message to the console.")
    .AddArgument<string>("Message", arg => {
        arg.AddDescription("The message to repeat.")
            .SetDefaultValue("Hello World");
    })
    .SetHandler(EchoHandler.Handle)
);

builder.AddCommand("repeat",
    cmdOptions => cmdOptions
    .AddCommandDescription("Repeat a message to the console a number of times.")
    .AddArgument<string>("Message", arg => {
        arg.AddDescription("The message to repeat.")
            .AddArity(ArgumentArity.ZeroOrOne);
    })
    .AddOption<int>("-rn", options => {
        options.AddAlias("--repeat-number")
            .AddDesccription("Number of times to repeat.");
    })
    .AddOption<int>("-w", options => {
        options.AddDesccription("Wait times between messages (in milliseconds).");
    })
    .SetHandler(RepeatHandler.HandleAsync)
);

builder.Execute(args);