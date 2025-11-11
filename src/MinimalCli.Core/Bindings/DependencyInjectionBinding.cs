using System;

namespace MinimalCli.Bindings;

public record DependencyInjectionBinding(string ParameterName, Type ParameterType)
    : ParameterBinding(ParameterName, ParameterType)
{
}
