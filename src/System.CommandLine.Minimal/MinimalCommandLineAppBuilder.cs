using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace System.CommandLine.Minimal;

public class MinimalCommandLineAppBuilder
{
    public MinimalCommandLineAppBuilder()
    {
        Services = new ServiceCollection();
        ConfigurationManager = new ConfigurationManager();
    }
    
    public ServiceCollection Services { get; private set; }
    public ConfigurationManager ConfigurationManager { get; private set; }

    public MinimalCommandLineApp Build()
    {
        var configRoot = ((IConfigurationBuilder)ConfigurationManager).Build();

        Services.AddSingleton<IConfiguration>(configRoot);

        var services = Services.BuildServiceProvider();

        return new MinimalCommandLineApp(services, configRoot);
    }
}
