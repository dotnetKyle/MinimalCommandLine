using DemoApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Minimal;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using DemoApp.Commands;
using Microsoft.Extensions.Hosting;

var app = new MinimalCommandLineBuilder()
    .ConfigureServices((services) =>
    {
        services
            .AddSingleton<ISerialNumberProvider, FileSerialNumberProvider>()
            .AddSingleton<CountryCompletions>();
    })
    // replace the root command with some help text
    .OverrideRootCommand<MainHelp>()
    // add commands
    .AddCommand<RootCACommand>()
    .AddCommand<IntermediateCACommand>()
    .AddCommand<SslCertificateCommand>()
    .Build();

await app.InvokeAsync(args);