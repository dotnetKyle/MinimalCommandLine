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
        services.AddTransient<ISerialNumberProvider, FileSerialNumberProvider>()
            .AddTransient<RootCaGenerator>()
            .AddTransient<IntermediateCaGenerator>()
            .AddTransient<SSLCertificateGenerator>()
            .AddSingleton<ISerialNumberProvider, FileSerialNumberProvider>()
            .AddSingleton<CountryCompletions>()
            ;
    })
    .OverrideRootCommand<MainHelp>()
    .AddCommand<RootCACommand>()
    .AddCommand<IntermediateCACommand>()
    .AddCommand<SslCertificateCommand>()
    .Build();

await app.InvokeAsync(args);