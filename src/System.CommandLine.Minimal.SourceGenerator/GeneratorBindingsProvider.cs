using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine.Minimal.SourceGeneration.Conventions;
using System.Linq;
using System.Threading;

namespace System.CommandLine.Minimal.SourceGeneration;

internal enum BindingType { Argument, Option, DependencyInjection }

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

        AttributeData? handlerAttribute = ctx.Attributes.FirstOrDefault(a => a.AttributeClass is not null 
            && a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.CommandLine.Minimal.HandlerAttribute");
        string? commandName = handlerAttribute?.ConstructorArguments.FirstOrDefault().Value as string;

        Location? argumentLocation = null;
        if (ctx.TargetNode is MethodDeclarationSyntax methodDecl)
        {
            AttributeSyntax? handlerAttrSyntax = methodDecl.AttributeLists
                .SelectMany(attrs => attrs.Attributes)
                .FirstOrDefault(attr => ctx.SemanticModel
                    .GetTypeInfo(attr)
                    .Type?
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.CommandLine.Minimal.HandlerAttribute"
                );
            argumentLocation = handlerAttrSyntax?.ArgumentList?.Arguments.FirstOrDefault()?.GetLocation();
        }
        //// Find the AttributeSyntax node for the HandlerAttribute
        //AttributeSyntax? handlerAttributeSyntax = ctx.TargetNode switch
        //{
        //    MethodDeclarationSyntax methodDecl => methodDecl.AttributeLists
        //        .SelectMany(list => list.Attributes)
        //        .FirstOrDefault(attr =>
        //            ctx.SemanticModel.GetTypeInfo(attr).Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        //            == "global::System.CommandLine.Minimal.HandlerAttribute"),
        //    _ => null
        //};
        //Location? argumentLocation = handlerAttributeSyntax?.ArgumentList?.Arguments.FirstOrDefault()?.GetLocation();


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

            string name = param.Name;
            string type = param.Type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);

            AttributeData? fromServicesAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "System.CommandLine.Minimal.FromServicesAttribute");
            AttributeData? optionAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "System.CommandLine.Minimal.OptionAttribute");
            AttributeData? argumentAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "System.CommandLine.Minimal.ArgumentAttribute");

            // check for attributes first, then try to bind anyways
            // by convention if it has a default value it is an option, developer can override this with an [Argument] attribute
            BindingType bindingType = DetermineBindingType(fromServicesAttribute, optionAttribute, argumentAttribute, param);

            if (bindingType == BindingType.DependencyInjection)
            {
                bindings.BindServiceDescriptor(name, type);
            }
            else if(bindingType == BindingType.Option)
            {
                bindings.BindOption(name, type, GetSymbolDefaultValue(ctx, param), IsParamCollectionType(param));
            }
            else if (bindingType == BindingType.Argument)
            {
                bindings.BindArgument(name, type, GetSymbolDefaultValue(ctx, param), IsParamCollectionType(param));
            }
        }

        return new GeneratingCommandBinder(
            CommandName: commandName,
            ClassNamespace: classNamespace,
            ClassName: className,
            MethodName: methodName,
            MethodReturnType: methodReturnType,
            MethodIsStatic: methodIsStatic,
            CommandNameLocation: argumentLocation,
            Bindings: bindings.ToImmutableArray());
    }

    private static BindingType DetermineBindingType(
        AttributeData? fromServicesAttribute,
        AttributeData? optionAttribute,
        AttributeData? argumentAttribute,
        IParameterSymbol param)
    {
        if (fromServicesAttribute is not null)
            return BindingType.DependencyInjection;
        
        if (optionAttribute is not null)
            return BindingType.Option;
        
        if (argumentAttribute is not null)
            return BindingType.Argument;
        
        // by convention, bind arrays to Options
        if (param.Type.Kind == SymbolKind.ArrayType)
            return BindingType.Option;
        
        // by convention, bind parameters with default values to Options
        if (param.HasExplicitDefaultValue)
            return BindingType.Option;
        
        // by convention, bind regular required parameters to Arguments
        return BindingType.Argument;
    }

    private static bool IsParamCollectionType(IParameterSymbol param)
    {
        // specifically exclude strings from this list
        if (param.Type.SpecialType == SpecialType.System_String)
            return false;

        if (param.Type.Kind == SymbolKind.ArrayType)
            return true;

        if (param.Type is INamedTypeSymbol namedType)
        {
            string typeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            foreach(INamedTypeSymbol @interface in namedType.AllInterfaces)
            {
                // Check if it's a constructed generic type like List<T>, IEnumerable<T>, etc.
                var constructedFrom = namedType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (constructedFrom is
                    "global::System.Collections.Generic.IEnumerable<T>" or
                    "global::System.Collections.Generic.IList<T>" or
                    "global::System.Collections.Generic.ICollection<T>" or
                    "global::System.Collections.Generic.IReadOnlyList<T>" or
                    "global::System.Collections.Generic.IReadOnlyCollection<T>" or
                    "global::System.Collections.Generic.List<T>")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void BindOption(this List<ParameterBinding> list, string name, string type, string? defaultValueCode, bool isCollectionType)
    {
        // TODO: add aliases
        OptionBinding option = new(name, type, defaultValueCode, isCollectionType);
        list.Add(option);
    }
    private static void BindArgument(this List<ParameterBinding> list, string name, string type, string? defaultValueCode, bool isCollectionType)
    {
        // TODO: add description attribute if applicable
        ArgumentBinding argument = new(name, type, defaultValueCode, isCollectionType);
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

internal abstract record ParameterBinding(string OriginalParameterName, string Type, string? DefaultValueConstant, bool IsCollectionType)
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
internal record ArgumentBinding(string OriginalParameterName, string Type, string? DefaultValueConstant, bool IsCollectionType)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant, IsCollectionType)
{
    /// <summary>
    /// The Argument name in title case, e.g. "myParameterName" becomes "My Parameter Name"
    /// </summary>
    public string ConventionalArgumentName 
        => ParameterNameConversion.ToArgumentName(this.OriginalParameterName);
}
internal record OptionBinding(string OriginalParameterName, string Type, string? DefaultValueConstant, bool IsCollectionType)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant, IsCollectionType)
{
    /// <summary>
    /// The option name in kebab case, e.g. "myParameterName" becomes "--my-parameter-name"
    /// </summary>
    public string OptionName => ParameterNameConversion.ToOptionName(this.OriginalParameterName);
}
internal record FromServicesBinding(string OriginalParameterName, string Type)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant:null, IsCollectionType:false)
{ 
}
