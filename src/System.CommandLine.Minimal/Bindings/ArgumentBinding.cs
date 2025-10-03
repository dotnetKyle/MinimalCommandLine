namespace System.CommandLine.Minimal.Bindings
{
    public record ArgumentBinding(string ParameterName, Type ParameterType, Argument Argument)
        : ParameterBinding(ParameterName, ParameterType)
    {
    }
}
