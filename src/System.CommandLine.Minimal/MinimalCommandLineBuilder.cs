using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace System.CommandLine.Minimal;

public class MinimalCommandLineBuilder : IHostApplicationBuilder
{ 
    private readonly string[] args;
    internal readonly HostApplicationBuilder builder;

    public MinimalCommandLineBuilder(params string[] args)
    {
        this.args = args ?? Array.Empty<string>();
        this.builder = Host.CreateApplicationBuilder();
        this.Properties = new Dictionary<object, object>();
    }
    
    public IServiceCollection Services => builder.Services;
    public IConfigurationManager Configuration => builder.Configuration;
    public IDictionary<object, object> Properties { get; }
    public IHostEnvironment Environment => builder.Environment;
    public ILoggingBuilder Logging => builder.Logging;
    public IMetricsBuilder Metrics => builder.Metrics;

    public MinimalCommandLineApp Build()
    {
        MinimalCommandLineApp app = new(this, this.args);
        return app;
    }

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) 
        where TContainerBuilder : notnull
    {
        throw new NotImplementedException();
    }
}
