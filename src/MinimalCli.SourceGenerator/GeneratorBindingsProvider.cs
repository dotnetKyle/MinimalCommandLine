using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;

namespace MinimalCli.SourceGeneration;

internal enum BindingType { Argument, Option, DependencyInjection }

internal static class GeneratorBindingsProvider
{
    /// <summary>
    /// The transform is the part where we gather the information from the function that we need later.
    /// This information should be deterministic and should not change if the underlying method signature doesn't change.
    /// </summary>
    public static GeneratingCommandBinder Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.TargetSymbol is not IMethodSymbol methodSymbol)
            return null!;

        const string globalHandlerAttribute = "global::MinimalCli.HandlerAttribute";

        //AttributeData? rootHandlerAttribute = ctx.Attributes.FirstOrDefault(a => a.AttributeClass is not null
        //    && a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::MinimalCli.RootHandlerAttribute");
        AttributeData? handlerAttribute = ctx.Attributes.FirstOrDefault(a => a.AttributeClass is not null 
            && a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == globalHandlerAttribute);
        string? commandName = handlerAttribute?.ConstructorArguments.FirstOrDefault().Value as string;

        Location? argumentLocation = null;
        if (ctx.TargetNode is MethodDeclarationSyntax methodDecl)
        {
            AttributeSyntax? handlerAttrSyntax = methodDecl.AttributeLists
                .SelectMany(attrs => attrs.Attributes)
                .FirstOrDefault(attr => ctx.SemanticModel
                    .GetTypeInfo(attr)
                    .Type?
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == globalHandlerAttribute
                );
            argumentLocation = handlerAttrSyntax?.ArgumentList?.Arguments.FirstOrDefault()?.GetLocation();
        }

        string classNamespace = methodSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string className = methodSymbol.ContainingType.Name;
        string methodName = methodSymbol.Name;
        string methodReturnType = methodSymbol.ReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        bool methodIsStatic = methodSymbol.IsStatic;

        ImmutableArray<ParameterBinding> bindings = GetParameterBindings(ctx, methodSymbol.Parameters);

        return new GeneratingCommandBinder(
            CommandName: commandName,
            ClassNamespace: classNamespace,
            ClassName: className,
            MethodName: methodName,
            MethodReturnType: methodReturnType,
            MethodIsStatic: methodIsStatic,
            CommandNameLocation: argumentLocation,
            Bindings: bindings);
    }


    /// <summary>
    /// The transform is the part where we gather the information from the function that we need later.
    /// This information should be deterministic and should not change if the underlying method signature doesn't change.
    /// </summary>
    public static GeneratingRootCommandBinder TransformForRoot(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.TargetSymbol is not IMethodSymbol methodSymbol)
            return null!;

        const string globalRootHandlerAttribute = "global::MinimalCli.RootHandlerAttribute";


        AttributeData? rootHandlerAttribute = ctx.Attributes.FirstOrDefault(a => a.AttributeClass is not null
            && a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == globalRootHandlerAttribute);

        // may want to do an analyzer warning if this method has both a RootHandler attribute and a Handler attribute.
        //AttributeData? handlerAttribute = ctx.Attributes.FirstOrDefault(a => a.AttributeClass is not null
        //    && a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::MinimalCli.HandlerAttribute");

        Location? argumentLocation = null;
        if (ctx.TargetNode is MethodDeclarationSyntax methodDecl)
        {
            AttributeSyntax? handlerAttrSyntax = methodDecl.AttributeLists
                .SelectMany(attrs => attrs.Attributes)
                .FirstOrDefault(attr => ctx.SemanticModel
                    .GetTypeInfo(attr)
                    .Type?
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == globalRootHandlerAttribute
                );
            argumentLocation = handlerAttrSyntax?.GetLocation();
        }

        string classNamespace = methodSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string className = methodSymbol.ContainingType.Name;
        string methodName = methodSymbol.Name;
        string methodReturnType = methodSymbol.ReturnType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);
        bool methodIsStatic = methodSymbol.IsStatic;

        ImmutableArray<ParameterBinding> bindings = GetParameterBindings(ctx, methodSymbol.Parameters);

        return new GeneratingRootCommandBinder(
            ClassNamespace: classNamespace,
            ClassName: className,
            MethodName: methodName,
            MethodReturnType: methodReturnType,
            MethodIsStatic: methodIsStatic,
            CommandNameLocation: argumentLocation,
            Bindings: bindings);
    }

    private static ImmutableArray<ParameterBinding> GetParameterBindings(GeneratorAttributeSyntaxContext ctx, ImmutableArray<IParameterSymbol> parameters)
    {
        List<ParameterBinding> bindings = new();

        foreach (IParameterSymbol param in parameters)
        {
            ImmutableArray<AttributeData> attributes = param.GetAttributes();

            string name = param.Name;
            string type = param.Type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);

            AttributeData? fromServicesAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "MinimalCli.FromServicesAttribute");
            AttributeData? optionAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "MinimalCli.OptionAttribute");
            AttributeData? argumentAttribute = attributes.FirstOrDefault(a => a.AttributeClass
                ?.ToDisplayString() == "MinimalCli.ArgumentAttribute");

            // check for attributes first, then try to bind anyways
            // by convention if it has a default value it is an option, developer can override this with an [Argument] attribute
            BindingType bindingType = DetermineBindingType(fromServicesAttribute, optionAttribute, argumentAttribute, param);

            if (bindingType == BindingType.DependencyInjection)
            {
                bindings.BindServiceDescriptor(name, type);
            }
            else if (bindingType == BindingType.Option)
            {
                bindings.BindOption(name, type, GetSymbolDefaultValue(ctx, param), IsParamCollectionType(param));
            }
            else if (bindingType == BindingType.Argument)
            {
                bindings.BindArgument(name, type, GetSymbolDefaultValue(ctx, param), IsParamCollectionType(param));
            }
        }

        return bindings.ToImmutableArray();
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
        static string FormatLiteral(object? value)
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

        if (param.HasExplicitDefaultValue)
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
