using System.Threading;
using System.Threading.Tasks;

namespace System.CommandLine.Minimal.Bindings
{
    public abstract class CommandOptions
    {
        public abstract Command Command { get; }
        
        //public Type? HandlerTarget { get; set; } = null;
        //public Delegate? Handler { get; set; } = null;

        public abstract ParameterBinding[] SetupCommandParameterBindings();

        public abstract Func<ParseResult, CancellationToken, Task<int>> Handler(IServiceProvider services);
    }
}
