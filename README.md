# System.CommandLine.Minimal

> A source generator that sits on top of the `System.CommandLine` namespace 
> to give an experience similar to the ASP.Net Core minimal API builders. This library 
> uses the Hosting libraries so a dotnet developer feels at home.
> 
> ### Primary Goal:
> 
> The primary goal of this library is to give a developer the power to get started with 
> System.CommandLine and to create commands with minimal boilerplate. All you have to do 
> to turn a function into a command is decorate it with the `[Command]` attribute!

## Getting Started

### Hello World:

First, create a new console application, then create a class to house the logic for your command:

```csharp
using System.CommandLine.Minimal;

public class HelloWorld
{
    // Just decorate the method with the command attribute!
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

This will map the HelloWorld class's `void Execute(..)` function to a command called `"hello"`, and it 
will map the parameter `message` to a string Argument called `<Message>`.

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

### Simple Command - Defaults:

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

This registers the following:
 * A command called `mycommand` to the handler `void Run(..)`.
 * The parameter `myArgument` to an Argument called `<MyArgument>`.
 * The optional parameter `myOption` to an Option called `--my-option`. 

The command can be called like this:

```bash
mycommand "Foo" --my-option "Bar"
```

Conventionally a `System.CommandLine.Argument` is created when the parameter is required and 
a `System.CommandLine.Option` is created when the parameter is optional.

### Modifying Default Conventions:

A simple command with two Arguments: a required Argument and an optional Argument:

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

Note: the use of the `[Argument]` attribute tells the source generator to generate this 
optional parameter as an Argument instead of an Option.

### Command Documentation:

After adding a command, you can add documentation for your command in the Program.cs file. After 
registering a command, the source generator creates an extension method that you can use to modify 
the descriptions, aliases, default values, and any other System.CommandLine functionality.

```csharp
using System.CommandLine.Minimal;

// after creating a 'greet' command that accepts a 'name' argument:

var builder = new MinimalCommandLineBuilder(args)

// this is a source generated extension for modifying your command's configuration:
builder.MapGreetCommand(config => 
{
    // set the configuration for the overall command
    config.Command.Description = "Greets a person with a friendly message.";

    // set the docs for the command args and options:
    config.NameArgument.Description = "The name of the person to greet."

    config.ToneOption.Description = "The optional tone of the greeting, e.g. formal.";
});

var app = builder.Build();

await app.StartAsync();
```

Now, when you user runs the `-h` Option for your app, or `greet -h` for the greet command they 
will be presented with the documentation you have set up in the generated 
`MapMyCommand(config => ..)` extensions. See the Demo project for a more complex example of this.

### Dependency Injection:

Instance command classes are automatically registered for dependency injection and they are 
created via dependency injection as well.  You can use dependency injection with your commands 
so you can share logic across all commands.

```csharp
public class GreeterCommand
{
    private MyInjectedClass _myInjectedClass;    
    public GreeterCommand(MyInjectedClass myInjectedClass)
    {
        _myInjectedClass = myInjectedClass;
    }

    [Command("greet")]
    public async Task ExecuteAsync(string name, string? tone = null)
    {
        // ... use _myInjectedClass here
    }
}
```

or

```csharp
// a static class command
public static class GreeterCommand
{
    [Command("greet")]
    public static async Task ExecuteAsync(
        string name, 
        [FromServices] MyInjectedClass myInjectedClass,    
        string? tone = null)
    {
        // ... use myInjectedClass here
    }
}
```

### Complex Configuration Example:

Here is snippet from the demo project that demonstrates configuring a complex command used to 
create a certificate.

```csharp
MinimalCommandLineBuilder builder = new(args);

builder
    .MapSslCommand(configure => 
    {
        configure.Command.Description = "Create an SSL certificate.";

        configure.CommonNameArgument.Description = "Add a common name to the certificate's subject name.";

        configure.IssuerFilePath2Argument.Description = "Add the file path to the Issuer CA.";

        configure.DNSNamesOption.Description = "Add one or more DNS names.";
        configure.DNSNamesOption.Aliases.Add("-dns");

        configure.IPAddressesOption.Description = "Add one or more IP Addresses.";
        configure.IPAddressesOption.Aliases.Add("-ip");

        configure.OUsOption.Description = "Add one or more OUs to the certificate's subject name.";
        configure.OUsOption.Aliases.Add("-ou");

        configure.OrganizationOption.Description = "Add an Organization to the certificate's subject name.";
        configure.OrganizationOption.Aliases.Add("-o");

        configure.CountryOption.Description = "Add an Organization to the certificate's subject name.";
        configure.CountryOption.Aliases.Add("-c");

        configure.Public_filePathOption.Description = "Override the default export path for the public certificate.";
        configure.Public_filePathOption.Aliases.Add("-pub");
        configure.Public_filePathOption.DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "ssl-pub.pfx");

        configure.Private_filePathOption.Description = "Override the default export path for the private certificate.";
        configure.Private_filePathOption.Aliases.Add("-prv");
        configure.Private_filePathOption.DefaultValueFactory = _ => Path.Combine(Environment.CurrentDirectory, "ssl-prv.pfx");

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
