# MinimalCli

> A source generator that works with the `System.CommandLine` namespace 
> to give an experience similar to the ASP.Net Core minimal API builders. This library 
> uses the Hosting libraries so a dotnet developer feels at home.
> 
> ### Primary Goal:
> 
> The primary goal of this library is to give a developer the power to get started with 
> System.CommandLine and to create commands with minimal boilerplate. All you have to do 
> to turn a function into a command is decorate it with the `[Handler]` attribute!

# Getting Started

### Hello World:

First, create a new console application, then create a class to house the logic for your command:

```csharp
using MinimalCli;

public class HelloWorld
{
    // Just decorate the method with the RootHandler attribute!
    [RootHandler]
    public void Execute(string message)
    {
        Console.WriteLine("Hello World!  {0}", message);
    }
}
```

Next, setup your Console App's `Program.cs` file:

```csharp
using MinimalCli;

var builder = new MinimalCommandLineBuilder(args)

// this is a source generated extension that will map all your commands
builder.MapAllCommands();

var app = builder.Build();

await app.StartAsync();
```

This will map the HelloWorld class's `void Execute(..)` function to a command called `"hello"`, and it 
will map the parameter `message` to a string Argument called `<Message>`.

# Installation

## Easy: using the project template

Simply run the following using the dotnet cli:

```
dotnet new install MinimalCli.Templates
```

Then you should be able to create a new MinimalCli project like this:

```
dotnet new minCli -n MyMinimalCliApp
```

## Advanced: Manually installing MinimalCli NuGet package

Add a reference to the nuget package `MinimalCommandLine`.

- Via csproj: `<PackageReference Include="MinimalCommandLine" Version="2.0.0.36" />`
- Via dotnet cli: `dotnet package add MinimalCommandLine`
- Via Visual Studio Menu: 
    * Tools >
    * NuGet Package Manager > 
    * Manage NuGet Packages for Solution...
    * Search for "MinimalCommandLine"
    * Select the package
    * Select the project you want to install it into
    * Hit Install

# Examples

## Simple Examples:

### Simple Command - Defaults:

A simple command with an Argument and an Option:

```csharp
using MinimalCli;

public class MyCommand
{
    // use the handler attribute to add additional commands
    [Handler("my-command")]
    public void Run(string myArgument, string? myOption = null)
    {
        Console.WriteLine("Arg:{0}, Option:{0}", myArgument, myOption);
    }
}
```

This registers the following:
 * A command called `my-command` to the handler `void Run(..)`.
 * The parameter `myArgument` to an Argument called `<MyArgument>`.
 * The optional parameter `myOption` to an Option called `--my-option`. 

The command can be called like this:

```bash
my-command "Foo" --my-option "Bar"
```

Conventionally a `System.CommandLine.Argument` is created when the parameter is required and 
a `System.CommandLine.Option` is created when the parameter is optional.

### Modifying Default Conventions:

A simple command with two Arguments: a required Argument and an optional Argument:

```csharp
using MinimalCli;

public class MyCommand
{
    [Handler("my-command")]
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
using MinimalCli;

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

### Argument and Option Completions:

You can add auto-completion suggestions for arguments and options by providing a completion source. This is particularly useful for file paths, predefined values, or any context-sensitive completions.

First, define a static method in your command class that returns completion items:

```csharp
using MinimalCli;
using System.CommandLine.Completions;

public class MyData
{
    [Handler("my-command")]
    public void Execute(string csvFilePath)
    {
        if (File.Exists(csvFilePath))
        {
            // ... process the CSV file
        }
    }

    public static IEnumerable<CompletionItem> GetFileNameCompletions(CompletionContext ctx)
    {
        string searchPattern = Path.GetFileName(ctx.WordToComplete) + "*.csv";
        string[] files = Directory.GetFiles(Environment.CurrentDirectory, searchPattern);
        return files.Select(f => new CompletionItem(f));
    }
}
```

Then, in your `Program.cs` file, map the completion source to the specific argument or option:

```csharp
builder.MapMyDataCommand(options =>
{
    options.Command.Description = "Load something from a CSV file.";
    options.CsvFilePathArgument.Description = "The file path to the CSV file where the data is stored.";
    
    // Add the completion source to the argument:
    options.CsvFilePathArgument.CompletionSources.Add(MyData.GetFileNameCompletions);
});
```

Now when users type the command and press `Tab`, they will see CSV file suggestions based on the current directory. The completion source receives a `CompletionContext` that includes the partially typed word (`WordToComplete`), allowing you to provide intelligent, context-aware suggestions.

Note: Completion sources can return simple strings or `CompletionItem` objects. For options, you can add completion sources the same way using `options.YourOption.CompletionSources.Add(...)`.

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

    [Handler("greet")]
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
    [Handler("greet")]
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
