using HelloWorld;
using System.CommandLine.Minimal;

var app = new MinimalCommandLineBuilder()
    .OverrideRootCommand<Root>()
    .Build();

app.Invoke(args);