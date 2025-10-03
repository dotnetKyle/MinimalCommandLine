namespace System.CommandLine.Minimal.Bindings
{
    public record OptionBinding(string ParameterName, Type ParameterType, Option Option)
        : ParameterBinding(ParameterName, ParameterType)
    {
    }
}
