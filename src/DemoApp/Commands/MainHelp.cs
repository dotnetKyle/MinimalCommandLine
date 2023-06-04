using System.CommandLine;

namespace DemoApp.Commands;

public class MainHelp : RootCommand
{
    public MainHelp() 
    {
        Description = "Commands for creating certificates.";

        this.SetHandler(Execute);
    }

    void Execute()
    {
        // print some manual-like help text
        Console.WriteLine("Custom SSL Certificate Generator.");
        Console.WriteLine();
        Console.WriteLine("Best Practices:");
        Console.WriteLine(" Start by creating a Root Certificate Authority by using the command \"rootCA\".");
        Console.WriteLine(" Use that self-signed certificate to create an Intermediate Certificate Authority, then take the new Root CA offline.");
        Console.WriteLine(" Use that intermediate authority to create multiple SSL Certificates in a repeatable way.");
        Console.WriteLine();
        Console.WriteLine("Use the -h flag to get the help documentation for this tool or any Command individually.");
        Console.WriteLine();

        Console.WriteLine("Examples:");
        Console.WriteLine(@"  rootca ""VA Root CA"" -c US -o ""VA"" -ou ""Department of Veterans Affairs""");
        Console.WriteLine();
        Console.WriteLine(@"  rootca ""VA Root CA"" -c US -o ""VA"" -nb 6/4/2023 -na 6/4/2033 -rsa 4096");
        Console.WriteLine();
        Console.WriteLine(@"  intermediateCA ""VA Int CA"" intCa.pfx -c US -o VA -nb 6/4/2023 -na 6/4/2033 -rsa 4096");
        Console.WriteLine();
        Console.WriteLine(@"  ssl ""VA Int CA"" intCa.pfx -o VA -dns VA.gov -ip 192.168.1.1 -rsa 4096");
        Console.WriteLine();
        Console.WriteLine();

        // print the help for the tool
        this.Invoke("-h");
    }
}
