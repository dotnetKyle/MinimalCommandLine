using DemoApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Minimal;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

var builder = new MinimalCommandLineAppBuilder();


builder.Services
    .AddTransient<ISerialNumberProvider, FileSerialNumberProvider>()
    .AddTransient<RootCaGenerator>()
    .AddTransient<IntermediateCaGenerator>()
    .AddTransient<SSLCertificateGenerator>();

var app = builder.Build();

app.AddRootDescription("Commands for creating certificates.");

app.AddCommand("rootCA",
    commandOptions => 
    {
        commandOptions
            .AddCommandDescription("Create a self-signed root CA certificate.")
            .AddArgument<string>("CommonName", argument =>
                argument.AddHelpName("Common Name")
                    .AddDescription("Add a common name to the certificate's subject name.")
                )
            .AddOption<string[]>("-ou", option =>
                option.AddAlias("--organizational-unit")
                    .AddDescription("Add one or more OUs to the certificate's subject name.")
                )
            .AddOption<string?>("-o", option =>
                option.AddAlias("--organization")
                    .AddDescription("Add an Organization to the certificate's subject name.")
                )
            .AddOption<string?>("-c", option =>
                option.AddAlias("--country")
                    .AddDescription("Add an Organization to the certificate's subject name.")
                )
            .AddOption<string>("-fp", option =>
                option.AddAlias("--file-path")
                    .AddDescription("Override the default export path for the root CA.")
                    .AddDefaultValueFactory(() => Path.Combine(Environment.CurrentDirectory, "rootca.pfx"))
                )
            .AddOption<DateOnly?>("-nb", option =>
                option.AddAlias("--not-before")
                    .AddDescription("Add a date that the certificate cannot be used before.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow))
                )
            .AddOption<DateOnly>("-na", option =>
                option.AddAlias("--not-after")
                    .AddDescription("Add a date that the certificate cannot be used after.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)))
                )
            .AddOption<int>("-rsa", option =>
                option.AddAlias("--rsa-size-in-bits")
                    .AddDescription("Change the default RSA size (as measured in bits).")
                    .AddDefaultValue(2048)
                )
            .SetHandler(RootCaGenerator.GenerateRootCaAsync);
    });


app.MapCommand<IntermediateCaGenerator>("intermediateCA", 
    handler => handler.GenerateCaAsync,
    commandOptions => 
    { 
        commandOptions
            .AddCommandDescription("Create an intermediate CA certificate")

            .AddArgument<string>("CommonName", argument =>
                argument.AddHelpName("Common Name")
                    .AddDescription("Add a common name to the certificate's subject name.")
                )
            .AddArgument<string>("IssuerFilePath", argument => 
                argument.AddHelpName("Issuer File Path")
                    .AddDescription("Add the file path to the Issuer CA.")
                )
            .AddOption<string[]>("-ou", option =>
                option.AddAlias("--organizational-unit")
                    .AddDescription("Add one or more OUs to the certificate's subject name.")
                )
            .AddOption<string?>("-o", option =>
                option.AddAlias("--organization")
                    .AddDescription("Add an Organization to the certificate's subject name.")
                )
            .AddOption<string?>("-c", option =>
                option.AddAlias("--country")
                    .AddDescription("Add an Organization to the certificate's subject name.")
                )
            .AddOption<string>("-fp", option =>
                option.AddAlias("--file-path")
                    .AddDescription("Override the default export path for the root CA.")
                    .AddDefaultValueFactory(() => Path.Combine(Environment.CurrentDirectory, "rootca.pfx"))
                )
            .AddOption<DateOnly>("-nb", option =>
                option.AddAlias("--not-before")
                    .AddDescription("Add a date that the certificate cannot be used before.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow))
                )
            .AddOption<DateOnly>("-na", option =>
                option.AddAlias("--not-after")
                    .AddDescription("Add a date that the certificate cannot be used after.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5)))
                )
            .AddOption<int>("-rsa", option =>
                option.AddAlias("--rsa-size-in-bits")
                    .AddDescription("Change the default RSA size (as measured in bits).")
                    .AddDefaultValue(2048)
                );
    });


app.MapCommand<SSLCertificateGenerator>("ssl",
    handler => handler.GenerateSslCertAsync,
    commandOptions => 
    {
        commandOptions
            .AddCommandDescription("Create an SSL certificate.")

            .AddArgument<string>("CommonName", argument =>
                argument.AddHelpName("Common Name")
                    .AddDescription("Add a common name to the certificate's subject name.")
                )
            .AddArgument<string>("IssuerFilePath", argument =>
                argument.AddHelpName("Issuer File Path")
                    .AddDescription("Add the file path to the Issuer CA.")
                )
            .AddOption<string[]>("-dns", option =>
                option.AddAlias("--dns-name")
                    .AddDescription("Add one or more DNS names.")
                )
            .AddOption<string[]>("-ip", option =>
                option.AddAlias("--ip-addresses")
                    .AddDescription("Add one or more IP Addresses.")
                )
            .AddOption<string[]>("-ou", option =>
                option.AddAlias("--organizational-unit")
                    .AddDescription("Add one or more OUs to the certificate's subject name.")
                )
            .AddOption<string?>("-o", option =>
                option.AddAlias("--organization")
                    .AddDescription("Add an Organization to the certificate's subject name.")
                )
            .AddOption<string?>("-c", option =>
                option.AddAlias("--country")
                    .AddDescription("Add an Organization to the certificate's subject name.")
                )
            .AddOption<string>("-pub", option =>
                option.AddAlias("--public-file-path")
                    .AddDescription("Override the default export path for the public certificate.")
                    .AddDefaultValueFactory(() => Path.Combine(Environment.CurrentDirectory, "ssl-pub.pfx"))
                )
            .AddOption<string>("-prv", option =>
                option.AddAlias("--private-file-path")
                    .AddDescription("Override the default export path for the private certificate.")
                    .AddDefaultValueFactory(() => Path.Combine(Environment.CurrentDirectory, "ssl-prv.pfx"))
                )
            .AddOption<DateOnly>("-nb", option =>
                option.AddAlias("--not-before")
                    .AddDescription("Add a date that the certificate cannot be used before.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow))
                )
            .AddOption<DateOnly>("-na", option =>
                option.AddAlias("--not-after")
                    .AddDescription("Add a date that the certificate cannot be used after.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)))
                )
            .AddOption<int>("-rsa", option =>
                option.AddAlias("--rsa-size-in-bits")
                    .AddDescription("Change the default RSA size (as measured in bits).")
                    .AddDefaultValue(2048)
                );
    });

app.Execute(args);