using System;

namespace MinimalCli.Bindings;

public abstract record ParameterBinding(string ParameterName, Type ParameterType);
