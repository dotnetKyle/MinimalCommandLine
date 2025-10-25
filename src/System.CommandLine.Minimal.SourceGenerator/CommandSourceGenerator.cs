using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine.Minimal.SourceGenerator;
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
                "System.CommandLine.Minimal.HandlerAttribute",
                predicate: MethodDeclPredicate,
                transform: GeneratorBindingsProvider.Transform
            ).Collect();

        context.RegisterSourceOutput(bindersProvider, (spc, binders) => {
            HashSet<string> commandNames = new();

            // first generate and create all of the CommandOptions classes
            foreach (GeneratingCommandBinder? binder in binders)
            {
                if(binder.CommandName is null || string.IsNullOrWhiteSpace(binder.CommandName))
                {
                    spc.ReportCommandNameEmptyError(binder.CommandNameLocation);
                    continue;
                }
                if(commandNames.Contains(binder.CommandName))
                {
                    spc.ReportCommandNameConflict(binder.CommandNameLocation, binder.CommandName);
                    continue;
                }

                // command name is validated so add to the hashset
                commandNames.Add(binder.CommandName);

                string? code = CommandOptionsWriter.GenerateOptions(binder);
                if(code is not null)
                {
                    spc.AddSource($"{binder.ClassName}_{binder.MethodName}_Command.g.cs", code);
                }
            }
            
            // Emit the aggregated Register method
            string? registryCode = MapAllCommandsExtensionWriter.GenerateMapAllCommandsExt(binders);
            if(registryCode is not null)
            {
                spc.AddSource("MapAllCommandsExtension.g.cs", registryCode);                
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
