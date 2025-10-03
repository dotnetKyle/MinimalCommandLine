using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Threading;

namespace System.CommandLine.Minimal.SourceGeneration;

[Generator]
public class CommandSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // always generate this code
        //context.RegisterPostInitializationOutput(GenerateMainFunctionCode);

        // attributes
        IncrementalValueProvider<ImmutableArray<GeneratingCommandBinder>> bindersProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "System.CommandLine.Minimal.CommandAttribute",
                predicate: MethodDeclPredicate,
                transform: GeneratorCommandProvider.Transform
            ).Collect();

        context.RegisterSourceOutput(bindersProvider, (spc, binders) => {
            
            // first generate and create all of the CommandOptions classes
            foreach (GeneratingCommandBinder? binder in binders)
            {
                string? code = CommandOptionsWriter.GenerateOptions(binder);
                if(code is not null)
                {
                    spc.AddSource($"{binder.ClassName}_{binder.MethodName}_Command.g.cs", code);
                }
            }

            // Emit the aggregated Register method
            string? registryCode = CommandRegistrationWriter.GenerateRegistrations(binders);
            if(registryCode is not null)
            {
                spc.AddSource("RegisterCommandsHook.g.cs", registryCode);                
            }
        });


    }

    internal static bool MethodDeclPredicate(SyntaxNode node, CancellationToken _) 
        => node is MethodDeclarationSyntax;

    //private static void GenerateMainFunctionCode(IncrementalGeneratorPostInitializationContext context)
    //{
    //    context.AddSource(
    //        "RegisterCommandsHook.g.cs",
    //        """
    //        using System.CommandLine;
    //        using System.CommandLine.Minimal;

    //        namespace System.CommandLine.Minimal;

    //        public static partial class RegisterCommandsHook
    //        {
    //            public static partial void Register(MinimalCommandLineBuilder builder);
    //        }
    //        """
    //        );
    //}
}
