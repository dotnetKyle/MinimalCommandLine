using DemoApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Minimal;

MinimalCommandLineBuilder builder = new(args);

builder.Services
    .AddTransient<ISerialNumberProvider, FileSerialNumberProvider>();

builder
    .MapRootCommand(configure =>
    {
        configure.Command.Description = "Create a self-signed root CA certificate.";

        configure.CommonNameArgument.Description = "Add a common name to the certificate's subject name.";
        configure.CommonNameArgument.Description = "The common name for the certificate";

        configure.OUsOption.Aliases.Add("-ou");
        configure.OUsOption.Description = "Add one or more OUs to the certificate's subject name.";

        configure.OrganizationOption.Aliases.Add("-o");
        configure.OrganizationOption.Description = "Add an Organization to the certificate's subject name.";

        configure.CountryOption.Aliases.Add("-c");
        configure.CountryOption.Description = "Add an Organization to the certificate's subject name.";

        configure.FilePathOption.Aliases.Add("-fp");
        configure.FilePathOption.Description = "Override the default export path for the root CA.";
        configure.FilePathOption.DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "rootca.pfx");

        configure.NotBeforeDateOption.Aliases.Add("-nb");
        configure.NotBeforeDateOption.Description = "Add a date that the certificate cannot be used before.";
        configure.NotBeforeDateOption.DefaultValueFactory = _ => DateOnly.FromDateTime(DateTime.UtcNow);

        configure.NotAfterDateOption.Aliases.Add("-na");
        configure.NotAfterDateOption.Description = "Add a date that the certificate cannot be used after.";
        configure.NotAfterDateOption.DefaultValueFactory = _ => DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10));

        configure.RsaSizeInBitsOption.Aliases.Add("-rsa");
        configure.RsaSizeInBitsOption.Description = "Change the default RSA size (as measured in bits).";
        configure.RsaSizeInBitsOption.DefaultValueFactory = _ => 2048;
    })
    .MapIntermediateCommand(configure => 
    {
        configure.Command.Description = "Create an intermediate CA certificate.";

        configure.CommonNameArgument.Description = "Add a common name to the certificate's subject name.";

        configure.IssuerFilePathArgument.Description = "Add the file path to the Issuer CA.";
        configure.IssuerFilePathArgument.CompletionSources.Add(ctx =>
        {
            return Directory.EnumerateFiles(Environment.CurrentDirectory, $"{ctx.WordToComplete}*.pfx");
        });

        configure.OUsOption.Description = "Add one or more OUs to the certificate's subject name.";

        configure.OrganizationOption.Description = "Add an Organization to the certificate's subject name.";
        configure.OrganizationOption.Aliases.Add("-o");

        configure.CountryOption.Description = "Add an Organization to the certificate's subject name.";
        configure.CountryOption.Aliases.Add("-c");

        configure.FilePathOption.Description = "Override the default export path for the root CA.";
        configure.FilePathOption.Aliases.Add("-fp");
        configure.FilePathOption.DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "rootca.pfx");

        configure.NotBeforeDateOption.Description = "Add a date that the certificate cannot be used before.";
        configure.NotBeforeDateOption.Aliases.Add("-nb");
        configure.NotBeforeDateOption.DefaultValueFactory = _ => DateOnly.FromDateTime(DateTime.UtcNow);

        configure.NotAfterDateOption.Description = "Add a date that the certificate cannot be used after.";
        configure.NotAfterDateOption.Aliases.Add("-na");
        configure.NotAfterDateOption.DefaultValueFactory = _ => DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5));

        configure.RsaSizeInBitsOption.Description = "Change the default RSA size (as measured in bits).";
        configure.RsaSizeInBitsOption.Aliases.Add("-rsa");
        configure.RsaSizeInBitsOption.DefaultValueFactory = _ => 2048;
    })
    .MapSslCommand(configure => 
    {
        configure.Command.Description = "Create an SSL certificate.";

        configure.CommonNameArgument.Description = "Add a common name to the certificate's subject name.";

        configure.IssuerFilePath2Argument.Description = "Add the file path to the Issuer CA.";

        configure.DnsNamesOption.Description = "Add one or more DNS names.";
        configure.DnsNamesOption.Aliases.Add("-dns");

        configure.IpAddressesOption.Description = "Add one or more IP Addresses.";
        configure.IpAddressesOption.Aliases.Add("-ip");

        configure.OUsOption.Description = "Add one or more OUs to the certificate's subject name.";
        configure.OUsOption.Aliases.Add("-ou");

        configure.OrganizationOption.Description = "Add an Organization to the certificate's subject name.";
        configure.OrganizationOption.Aliases.Add("-o");

        configure.CountryOption.Description = "Add an Organization to the certificate's subject name.";
        configure.CountryOption.Aliases.Add("-c");

        configure.Public_FilePathOption.Description = "Override the default export path for the public certificate.";
        configure.Public_FilePathOption.Aliases.Add("-pub");
        configure.Public_FilePathOption.DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "ssl-pub.pfx");

        configure.Private_FilePathOption.Description = "Override the default export path for the private certificate.";
        configure.Private_FilePathOption.Aliases.Add("-prv");
        configure.Private_FilePathOption.DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "ssl-prv.pfx");

        configure.NotBeforeDateOption.Description = "Add a date that the certificate cannot be used before.";
        configure.NotBeforeDateOption.Aliases.Add("-nb");
        configure.NotBeforeDateOption.DefaultValueFactory = _ => DateOnly.FromDateTime(DateTime.UtcNow);

        configure.NotAfterDateOption.Description = "Add a date that the certificate cannot be used after.";
        configure.NotAfterDateOption.Aliases.Add("-na");
        configure.NotAfterDateOption.DefaultValueFactory = _ => DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        configure.RsaSizeInBitsOption.Description = "Change the default RSA size (as measured in bits).";
        configure.RsaSizeInBitsOption.Aliases.Add("-rsa"); 
        configure.RsaSizeInBitsOption.DefaultValueFactory = _ => 2048;
    });

builder.MapAllCommands();

if(args.Any(arg => arg.Equals("--useShell", StringComparison.OrdinalIgnoreCase)))
{
    builder.UseShellMode();
}

MinimalCommandLineApp app = builder.Build();

app.AddPrompt("CERTS");

app.AddRootDescription("Commands for creating certificates.");

app.SetRootHandler(() => {
    Console.WriteLine("Start by creating a Root Certificate Authority by using the " +
        "command \"rootCA\".");
    Console.WriteLine("Then you can use that self-signed certificate to create " +
        "an Intermediate Certificate Authority, then an SSL Certificate.");
    Console.WriteLine("Use the -h flag to show the help documentation. Also, " +
        "using -h on any command will show the help for that command.");
    Console.WriteLine();
});

await app.StartAsync();
