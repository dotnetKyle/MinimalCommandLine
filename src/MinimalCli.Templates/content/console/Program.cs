// See https://github.com/dotnetKyle/MinimalCommandLine for more information
using MinimalCli;

var builder = new MinimalCommandLineBuilder(args);

// configure the root command's options
builder.MapRootCommand(options => {
    options.Command.Description = "A greeter command";

    options.YourNameOption.Description = "Your name so the command can greet you.";
});

// modify the echo command's options
builder.MapEchoCommandCommand(options =>
{
    options.Command.Description = "A command that echoes a prompt back to the console";

    options.PromptArgument.Description = "The prompt to echo back";
    options.PromptArgument.DefaultValueFactory = _ => "echo";
});


builder.MapAllCommands();

var app = builder.Build();

await app.StartAsync();
