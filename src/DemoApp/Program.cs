using System.CommandLine.Minimal;
using DemoApp.Handlers;

//await
new MinimalCommandLineBuilder()
    .AddRootDescription("A collection of commands for printing to the console.")
    //.AddRootAlias("print")
    //.AddRootArgument<string>("mainArgName")
    //.AddRootOption<int>("-n")

    .AddCommand("echo",
        cmdOptions => cmdOptions
            .AddCommandDescription("Print a message to the console.")
            .AddArgument<string>("Message", "The message to repeat.")
            .SetHandler(EchoHandler.Handle)
    )

    .AddCommand("repeat",
        cmdOptions => cmdOptions
            .AddCommandDescription("Repeat a message to the console a number of times.")
            .AddArgument<string>("Message", "The message to repeat.")
            .AddOption<int>("-n", "Number of times to repeat.")
            .SetHandler((string message, int repeat) => 
            {
                for (int i = 0; i < repeat; i++)
                    Console.WriteLine(message);
            })
    )
    .Execute(args)
    //.ExecuteAsync(args)
    ;

