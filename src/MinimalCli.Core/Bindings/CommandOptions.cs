using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalCli.Bindings;

public abstract class CommandOptions
{
    public abstract Command Command { get; }
    
    public abstract ParameterBinding[] SetupCommandParameterBindings();

    public abstract Func<ParseResult, CancellationToken, Task<int>> Handler(IServiceProvider services);
}
