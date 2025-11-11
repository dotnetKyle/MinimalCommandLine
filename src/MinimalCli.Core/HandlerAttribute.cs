namespace MinimalCli;

[AttributeUsage(AttributeTargets.Method)]
public sealed class HandlerAttribute : Attribute
{
    public string Name { get; }
    public HandlerAttribute(string name) => Name = name;
}
