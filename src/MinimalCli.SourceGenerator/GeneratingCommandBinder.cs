using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using MinimalCli.SourceGeneration.Conventions;

namespace MinimalCli.SourceGeneration;

internal record GeneratingCommandBinder(
    string? CommandName,
    string ClassNamespace,
    string ClassName,
    string MethodName,
    string MethodReturnType,
    bool MethodIsStatic,
    Location? CommandNameLocation,
    ImmutableArray<ParameterBinding>? Bindings
)
{
    public string CommandHelpName => Conventions.ParameterNameConversion.ToArgumentName(this.CommandName ?? "");
    public string CommandNameTitleCase => this.CommandName?.ToSymbolName() ?? "";
    public string CommandOptionsName => $"{this.CommandNameTitleCase}CommandOptions";
    public string FullClassName => $"{this.ClassNamespace}.{this.ClassName}";
    public string FullMethodName => $"{this.FullClassName}.{this.MethodName}";
}

internal record GeneratingRootCommandBinder(
    string ClassNamespace,
    string ClassName,
    string MethodName,
    string MethodReturnType,
    bool MethodIsStatic,
    Location? CommandNameLocation,
    ImmutableArray<ParameterBinding>? Bindings
)
{
    public string CommandHelpName => "RootCommand";
    public string CommandOptionsName => $"RootCommandOptions";
    public string FullClassName => $"{this.ClassNamespace}.{this.ClassName}";
    public string FullMethodName => $"{this.FullClassName}.{this.MethodName}";
}
