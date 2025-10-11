namespace System.CommandLine.Minimal.Bindings;

public record DependencyInjectionBinding(string ParameterName, Type ParameterType)
    : ParameterBinding(ParameterName, ParameterType)
{
}
