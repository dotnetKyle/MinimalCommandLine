namespace MinimalCli;

/// <summary>
/// Designates this method as the root handler for the command. This will become the default command if no other command matches.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RootHandlerAttribute : Attribute
{
    public RootHandlerAttribute() { }
}
