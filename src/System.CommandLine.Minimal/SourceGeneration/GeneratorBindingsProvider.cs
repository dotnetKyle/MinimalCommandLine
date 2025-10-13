using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine.Minimal.SourceGeneration.Conventions;
using System.Linq;
using System.Threading;

namespace System.CommandLine.Minimal.SourceGeneration;

internal static class GeneratorBindingsProvider
{
    /// <summary>
    /// The transform is the part where we gather the information from the function that we need later.
    /// This information should be deterministic and should not change if the underling method signature doesn't change.
    /// </summary>
    public static GeneratingCommandBinder Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.TargetSymbol is not IMethodSymbol methodSymbol)
            return null!;

        AttributeData? commandAttribute = ctx.Attributes.FirstOrDefault(a => a.AttributeClass is not null 
            && a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.CommandLine.Minimal.CommandAttribute");
        string? commandName = commandAttribute?.ConstructorArguments.FirstOrDefault().Value as string;

        string classNamespace = methodSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string className = methodSymbol.ContainingType.Name;
        string methodName = methodSymbol.Name;
        string methodReturnType = methodSymbol.ReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        bool methodIsStatic = methodSymbol.IsStatic;

        ImmutableArray<IParameterSymbol> parameters = methodSymbol.Parameters;

        List<ParameterBinding> bindings = new();
        foreach(IParameterSymbol param in parameters)
        {
            ImmutableArray<AttributeData> attributes = param.GetAttributes();
            bool hasDefault = param.HasExplicitDefaultValue;
            string name = param.Name;
            string type = param.Type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);

            AttributeData? fromServicesAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "System.CommandLine.Minimal.FromServicesAttribute");
            AttributeData? optionAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "System.CommandLine.Minimal.OptionAttribute");
            AttributeData? argumentAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "System.CommandLine.Minimal.ArgumentAttribute");

            // check for attributes first, then try to bind anyways
            // by convention if it has a default value it is an option, devleoper can override this with an [Argument] attribute

            if (fromServicesAttribute is not null)
            {
                bindings.BindServiceDescriptor(name, type);
            }
            else if(optionAttribute is not null)
            {
                bindings.BindOption(name, type, GetSymbolDefaultValue(ctx, param));
            }
            else if(argumentAttribute is not null || !hasDefault)
            {
                bindings.BindArgument(name, type, GetSymbolDefaultValue(ctx, param));
            }
            else
            {
                bindings.BindOption(name, type, GetSymbolDefaultValue(ctx, param));
            }
        }

        return new GeneratingCommandBinder(
            CommandName: commandName,
            ClassNamespace: classNamespace,
            ClassName: className,
            MethodName: methodName,
            MethodReturnType: methodReturnType,
            MethodIsStatic: methodIsStatic,
            Bindings: bindings.ToImmutableArray());
    }

    private static void BindOption(this List<ParameterBinding> list, string name, string type, string? defaultValueCode)
    {
        // TODO: add aliases
        OptionBinding option = new(name, type, defaultValueCode);
        list.Add(option);
    }
    private static void BindArgument(this List<ParameterBinding> list, string name, string type, string? defaultValueCode)
    {
        // TODO: add description attribute if applicable
        ArgumentBinding argument = new(name, type, defaultValueCode);
        list.Add(argument);
    }
    private static void BindServiceDescriptor(this List<ParameterBinding> list, string name, string type)
    {
        FromServicesBinding fromServicesBinding = new(name, type);
        list.Add(fromServicesBinding);
    }

    private static string? GetSymbolDefaultValue(GeneratorAttributeSyntaxContext ctx, IParameterSymbol param)
    {
        static string FormatLiteral(object value)
        {
            return value switch
            {
                null => "null",
                string s => $"\"{s}\"",
                char c => $"'{c}'",
                bool b => b ? "true" : "false",
                Enum e => $"{e.GetType().FullName}.{e}",
                _ => value.ToString()
            };
        }

        if (param.HasExplicitDefaultValue && param.ExplicitDefaultValue is not null)
        {
            SyntaxReference? syntaxRef = param.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef?.GetSyntax() is ParameterSyntax paramSyntax && paramSyntax.Default is not null)
            {
                ExpressionSyntax? defaultExpr = paramSyntax.Default.Value;
                SymbolInfo symbolInfo = ctx.SemanticModel.GetSymbolInfo(defaultExpr);
                ISymbol? symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                // if the symbol is a field (like a defined constant)
                if(symbol is IFieldSymbol field && field.IsConst && field.HasConstantValue)
                {
                    if(field.Type.BaseType is not null && field.Type.BaseType.ToDisplayString() == "System.Enum")
                    {
                        // check if it's an enumeration
                        IFieldSymbol enumValue = field.Type.GetMembers()
                            .OfType<IFieldSymbol>()
                            .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, field.ConstantValue));

                        if(enumValue is not null)
                        {
                            return $"{field.Type.ToDisplayString()}.{enumValue.Name}";
                        }

                        return field.ToDisplayString();
                    }

                    return FormatLiteral(field.ConstantValue);
                }

                return symbol != null
                    ? symbol.OriginalDefinition.ToDisplayString()
                    : defaultExpr.ToString();
            }

            ParameterSyntax? defaultSyntax = param.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ParameterSyntax;

            return FormatLiteral(param.ExplicitDefaultValue);
        }

        return null;
    }
}

internal record GeneratingCommandBinder(
    string? CommandName,
    string ClassNamespace,
    string ClassName,
    string MethodName,
    string MethodReturnType,
    bool MethodIsStatic,
    ImmutableArray<ParameterBinding>? Bindings
)
{
    public string CommandHelpName => Conventions.ParameterNameConversion.ToArgumentName(this.CommandName ?? "");
    public string CommandNameTitleCase => this.CommandName?.ToSymbolName() ?? "";
    public string CommandOptionsName => $"{this.CommandNameTitleCase}CommandOptions";
    public string FullClassName => $"{this.ClassNamespace}.{this.ClassName}";
    public string FullMethodName => $"{this.FullClassName}.{this.MethodName}";
}

internal abstract record ParameterBinding(string OriginalParameterName, string Type, string? DefaultValueConstant)
{
    public string NameTitleCase
    {
        get
        {
            return this.OriginalParameterName.ToSymbolName();
        }
    }
    public string HelpName
    {
        get
        {
            // split and add spaces
            char c = char.ToUpper(this.OriginalParameterName[0]);
            return c + this.OriginalParameterName.Substring(1);
        }
    }
}
internal record ArgumentBinding(string OriginalParameterName, string Type, string? DefaultValueConstant)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant)
{
    /// <summary>
    /// The Argument name in title case, e.g. "myParameterName" becomes "My Parameter Name"
    /// </summary>
    public string ConventionalArgumentName 
        => Conventions.ParameterNameConversion.ToArgumentName(this.OriginalParameterName);
}
internal record OptionBinding(string OriginalParameterName, string Type, string? DefaultValueConstant)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant)
{
    /// <summary>
    /// The option name in kebab case, e.g. "myParameterName" becomes "--my-parameter-name"
    /// </summary>
    public string OptionName 
        => Conventions.ParameterNameConversion.ToOptionName(this.OriginalParameterName);
}
internal record FromServicesBinding(string OriginalParameterName, string Type)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant:null)
{ 
}
