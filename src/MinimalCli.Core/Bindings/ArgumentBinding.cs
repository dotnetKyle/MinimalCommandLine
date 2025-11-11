namespace MinimalCli.Bindings;

public record ArgumentBinding(string ParameterName, Type ParameterType, Argument Argument)
    : ParameterBinding(ParameterName, ParameterType)
{
}
