using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace System.CommandLine.Minimal;

public class MinimalCommandLineHostEnvironment : IHostEnvironment
{
    string environmentName = "";
    string applicationName = "";
    string contentRootPath = "";
    IFileProvider fileProvider = new NullFileProvider();

    MinimalCommandLineHostEnvironment() { }

    internal static MinimalCommandLineHostEnvironment CreateInstance(string environmentName, string applicationName, string contentRootPath)
    {
        return new MinimalCommandLineHostEnvironment
        {
            environmentName = environmentName,
            applicationName = applicationName,
            contentRootPath = contentRootPath,
            fileProvider = new PhysicalFileProvider(contentRootPath)
        };
    }

    public string EnvironmentName 
    { 
        get => environmentName; 
        set { } 
    }
    public string ApplicationName 
    { 
        get => applicationName; 
        set { } 
    }
    public string ContentRootPath 
    { 
        get => contentRootPath; 
        set { } 
    }
    public IFileProvider ContentRootFileProvider 
    { 
        get => fileProvider; 
        set { } 
    }
}
