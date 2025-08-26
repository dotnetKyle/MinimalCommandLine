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
    internal CommandExecutionMode cmdExecutionMode = CommandExecutionMode.CliDefault;

    public MinimalCommandLineBuilder(string[] args)
    {
        this.args = args ?? [];
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
        // add required services
        this.Services.AddSingleton<CommandExecutorCli>();
        this.Services.AddSingleton<CommandExecutorShell>();

        // pass in args from builder
        MinimalCommandLineApp app = new(this, this.args);

        return app;
    }

    /// <summary>
    /// Use shell mode as the default mode for execution.
    /// <para>The default is CLI Mode but shell mode is allowed.</para>
    /// </summary>
    public MinimalCommandLineBuilder UseShellMode()
    {
        this.cmdExecutionMode = CommandExecutionMode.ShellDefault;
        return this;
    }

    /// <summary>
    /// Disable CLI mode for execution, this requires shell mode for the user.
    /// <para>The default is CLI Mode but shell mode is allowed.</para>
    /// </summary>
    public MinimalCommandLineBuilder RequireShellMode()
    {
        this.cmdExecutionMode = CommandExecutionMode.ShellRequired;
        return this;
    }
    /// <summary>
    /// Disable shell mode for execution, this requires CLI mode for the user.
    /// <para>The default is CLI Mode but shell mode is allowed.</para>
    /// </summary>
    public MinimalCommandLineBuilder RequireCliMode()
    {
        this.cmdExecutionMode = CommandExecutionMode.CliRequired;
        return this;
    }

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) 
        where TContainerBuilder : notnull
    {
        throw new NotImplementedException();
    }
}
