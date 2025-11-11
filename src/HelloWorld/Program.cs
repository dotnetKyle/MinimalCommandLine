using MinimalCli;

MinimalCommandLineBuilder builder = new(args);

builder.MapAllCommands();

MinimalCommandLineApp app = builder.Build();

await app.StartAsync();
