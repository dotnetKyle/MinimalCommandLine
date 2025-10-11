# System.CommandLine.Minimal

> A set of minimal builders that sits on top of the `System.CommandLine` namespace 
> to give an experience similar to the ASP.Net Core minimal API builders. This library 
> uses the Hosting builders so a dotnet developer feels at home.
> 
> ### Primary Goal:
> 
> The primary goal of this library design is to give the developer the power to 
> easily with minimal effort get started with System.CommandLine and to create commands
> with minimal boilerplate.

### Hello World:

First, create a new console application, then create a class to house the logic for your command:

```csharp
using System.CommandLine.Minimal;

public class HelloWorld
{
    // The command attribute sets up source generation to recognize and map your 
    //   command, the handler, and all of it's parameters
    [Command("hello")]
    public void Execute(string message)
    {
        Console.WriteLine("Hello World!  {0}", message);
    }
}
```

Next, setup your Console App's `Program.cs` file:

```csharp
using System.CommandLine.Minimal;

var builder = new MinimalCommandLineBuilder(args)

// this is a source generated extension that will map all your commands
builder.MapAllCommands();

var app = builder.Build();

await app.StartAsync();
```

This will map the HelloWorld class's Execute function to a command called `hello` with a 
string Argument called `Message`.

## Installing MinimalCommandLine

Add a reference to the nuget package `MinimalCommandLine`.

- Via csproj: `<PackageReference Include="MinimalCommandLine" Version="0.5.0.10" />`
- Via dotnet cli: `dotnet package add MinimalCommandLine`
- Via Visual Studio Menu: 
    * Tools >
    * NuGet Package Manager > 
    * Manage NuGet Packages for Solution...
    * Search for "MinimalCommandLine"
    * Select the package
    * Select the project you want to install it into
    * Hit Install


## Simple Examples:

### Simple Command - Conventions

A simple command with an Argument and an Option:

```csharp
using System.CommandLine.Minimal;

public class MyCommand
{
    [Command("mycommand")]
    public void Run(string myArgument, string? myOption = null)
    {
        Console.WriteLine("Arg:{0}, Option:{0}", myArgument, myOption);
    }
}
```

This registers a command called `mycommand`, with an Argument called `MyArgument`, and an Option 
called `--my-option`. It can be called like this:

```bash
mycommand "Foo" --my-option "Bar"
```

Conventionally a `System.CommandLine.Argument` is created when the parameter is not optional and 
a `System.CommandLine.Option` is created when the parameter is optional.

### Modifying Conventions

A simple command with two Arguments a required Argument and an optional Argument:


```csharp
using System.CommandLine.Minimal;

public class MyCommand
{
    [Command("mycommand")]
    public void Execute(string myArgument, [Argument] string? myArgument2 = null)
    {
        Console.WriteLine("Arg:{0}, Arg2:{0}", myArgument, myArgument2);
    }
}
```

Note: the use of the `[Argument]` attribute tells the source generator to treat this 
Argument as optional.


### Documentation Examples:


The API and the application logic are together.  Uses an `Action<Task>` directly in the Program.cs.

```csharp
// Program.cs

MinimalCommandLineBuilder = new(args);

MinimalCommandLineApp app = builder.Build();

app.AddRootDescription("Create X509Certificates.");

// generate a root CA certificate
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
            .AddDescription(
              "Add one or more Organizational Units (OUs) to the certificate's subject name."
            )
          )
        .AddOption<DateOnly>("-na", option =>
          option.AddAlias("--not-after")
            .AddDescription("Add a date that the certificate cannot be used after.")
            .AddDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)))
          )
        // Bind the application logic here
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

await app.StartAsync();
```

### Separate Approach (static class):

Same logic as above but inside a static method allows for the parameters to 
have optional values (which are automatically to the API help convention).

```csharp
// Program.cs

MinimalCommandLineBuilder builder = new(args);

MinimalCommandLineApp app = builder.Build();

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
        // Use a static method for the application logic
        .SetHandler(RootCaGenerator.GenerateSelfSigned);
    });

await app.StartAsync();

public static class RootCaGenerator
{
  public static async Task GenerateSelfSigned(
      string commonName, 
      string[] OUs, 
      string organization = "Your Org Here")
  {
    // Truncated for brevity
  }
}
```

### Separate Approach (instance class with dependency injection):

Uses a class instance and gets dependencies from DI.

```csharp
// Program.cs


// add the command and it's dependencies to Dependency Injection (DI)
MinimalCommandLineBuilder builder = new(args)
  .AddTransient<ISerialNumberProvider, FileSystemSerialNumberProvider>()
  .AddTransient<IntermediateCaGenerator>();

MinimalCommandLineApp app = builder.Build();

app.AddRootDescription("Create X509Certificates.");

// generate a intermediateCA certificate
app.MapCommand<IntermediateCaGenerator>("intermediateCA", 
  // this parameter is a binder to map the command to the instance method containing the application logic
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
          .AddDescription("Add an issuer certificate with its private key.")
      )
      .AddOption<string[]>("-ou", option =>
        // truncated for brevity
    });

await app.StartAsync();


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
    // Truncated for brevity
  }
}
public class FileSystemSerialNumberProvider : IFileSystemSerialNumberProvider
{
  // Truncated for brevity
}
```






## Contributors - Getting Started

`git clone https://github.com/dotnetKyle/MinimalCommandLine.git`

### Using Visual Studio:

Set DemoApp as the startup project.

Check the Properties/launchSettings.json file, ensure that the `commandLineArgs` property is set to `-h`

### Using the dotnet CLI:

```shell
dotnet build DemoApp.csproj -c Debug

cd \bin\Debug\net8.0\

DemoApp.exe -h
```
