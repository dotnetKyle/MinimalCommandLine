using System;

namespace MinimalCli;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ArgumentAttribute : Attribute
{
}
