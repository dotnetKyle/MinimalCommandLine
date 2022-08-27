# System.CommandLine.Minimal

> A set of minimal builders that sits on top of the 
> `System.CommandLine` namespace to give an experience 
> similar to the ASP.Net Core minimal API builders.
> 
> ### Primary Goal:
> 
> The primary goal of this library design is to give the developer 
> the option to use one of the following approaches:
>  * **Separate Approach:** Separate the api design from the actual logic which allows for high testability.
>  * **Inline Approach:** To put the logic directly with the api design, which allows for maximum readability.

## Getting Started

`git clone https://github.com/dotnetKyle/MinimalCommandLine.git`

### Using Visual Studio:

Set DemoApp as the startup project.

Check the Properties/launchSettings.json file, ensure that the `commandLineArgs` are set to `-h`

### Using the dotnet CLI:

```shell
dotnet build DemoApp.csproj -c Debug

cd \bin\Debug\net6.0\

DemoApp.exe -h
```

## Simple Examples:

### Inline Approach:

The API and the application logic are together.  Uses an `Action<Task>` directly in the Program.cs.

```csharp
var app = new MinimalCommandLineAppBuilder()
	.Build();

app.AddRootDescription("Create X509Certificates.");

// generate a rootCA certificate
app.AddCommand("rootCA"
	cmdOptions => 
	{
		cmdOptions
			.AddCommandDescription("Create a self-signed root certificate authority.")
			.AddArgument<string>("CommonName", argument =>
				argument.AddHelpName("Common Name")
					.AddDescription("Add a common name to the certificate's subject name.")
			)
            .AddOption<string[]>("-ou", option =>
                option.AddAlias("--organizational-unit")
                    .AddDescription("Add one or more OUs to the certificate's subject name.")
                )
            .AddOption<DateOnly>("-na", option =>
                option.AddAlias("--not-after")
                    .AddDescription("Add a date that the certificate cannot be used after.")
                    .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)))
                )
            .SetHandler(async (string commonName, string[] OUs, DateOnly notAfter) =>
            {
                var notAfterDate = notAfter.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

                if (OUs is null)
                    OUs = Array.Empty<string>();

                var filePath = Path.Combine(Environment.CurrentDirectory, "rootCA.pfx");

                var subjectName = $"CN={commonName}";

                foreach (var ou in OUs)
                    subjectName += $", OU={ou}";

                subjectName += $", O=Your Org Name Here, C=USA";

                using (var rsa = RSA.Create(2048))
                {
                    var req = new CertificateRequest(
                        subjectName,
                        rsa,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    req.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(true, false, 0, true)
                    );

                    using (var cert = req.CreateSelfSigned(DateTime.UtcNow,notAfterDate))
                    {
                        var pfx = cert.Export(X509ContentType.Pfx);

                        await File.WriteAllBytesAsync(filePath, pfx);

                        Console.WriteLine(filePath);
                    }
                }
            })
	});
```

### Separate Approach (static class):

Same logic as above but inside a static method allows for the parameters to have optional values (which are automatically to the API help convention).

```csharp
var app = new MinimalCommandLineAppBuilder()
	.Build();

app.AddRootDescription("Create X509Certificates.");

// generate a rootCA certificate
app.AddCommand("rootCA"
	cmdOptions => 
	{
		cmdOptions
			.AddCommandDescription("Create a self-signed root certificate authority.")
			.AddArgument<string>("CommonName", argument =>
				argument.AddHelpName("Common Name")
					.AddDescription("Add a common name to the certificate's subject name.")
			)
            .AddOption<string[]>("-ou", option =>
                option.AddAlias("--organizational-unit")
                    .AddDescription("Add one or more OUs to the certificate's subject name.")
                )
            .AddOption<string>("-o", option =>
                option.AddAlias("--organization")
                    .AddDescription("Override the default organization name.")
                )
            // Use a static method for the logic
            .SetHandler(RootCaGenerator.GenerateSelfSigned);
    });

public static class RootCaGenerator
{
    public static async Task GenerateSelfSigned(string commonName, string[] OUs, string organization = "Your Org Here")
    {
        // Truncated for berevity
    }
}
```

### Separate Approach (instance class with dependency injection):

Uses a class instance and gets dependencies from DI.

```csharp
// add the command and it's dependencies to DI
var app = new MinimalCommandLineAppBuilder()
    .AddTransient<ISerialNumberProvider, FileSystemSerialNumberProvider>()
    .AddTransient<IntermediateCaGenerator>()
	.Build();

app.AddRootDescription("Create X509Certificates.");

// generate a intermediateCA certificate
app.MapCommand<IntermediateCaGenerator>("intermediateCA", 
    // this parameter is a binder to map the command to the instance method
    handler => handler.GenerateCaAsync,
	cmdOptions => 
	{
		cmdOptions
			.AddCommandDescription("Create a intermediate certificate authority.")
			.AddArgument<string>("CommonName", argument =>
				argument.AddHelpName("Common Name")
					.AddDescription("Add a common name to the certificate's subject name.")
			)
			.AddArgument<string>("IssuerCertificate", argument =>
				argument.AddHelpName("Issuer Certificate")
					.AddDescription("Add an issuer certificate with it's private key.")
			)
            .AddOption<string[]>("-ou", option =>
                // truncated for berevity
    });

public class IntermediateCaGenerator
{
    ISerialNumberProvider _serialNumberProvider;
    public IntermediateCaGenerator(ISerialNumberProvider serialNumberProvider)
    {
        _serialNumberProvider = serialNumberProvider;
    }

    public async Task GenerateCaAsync(string commonName, string issuerFilePath)
    {
        var certificateSerialNumber = _serialNumberProvider.NextSerialNumber();
        // Truncated for berevity
    }
}
public class FileSystemSerialNumberProvider : IFileSystemSerialNumberProvider
{
    // Truncated for berevity
}
```