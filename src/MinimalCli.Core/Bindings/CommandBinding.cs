using System.Collections.Generic;

namespace MinimalCli.Bindings;

public abstract class CommandBinding
{
    /// <summary>
    /// Used if there is an instance class that needs to be instantiated from dependency injection.
    /// </summary>
    public Type? CommandClass { get; set; }

    /// <summary>
    /// Used if this is a static method
    /// </summary>
    public Delegate? CommandDelegate { get; set; }

    public List<ParameterBinding> Parameters = new();
}
