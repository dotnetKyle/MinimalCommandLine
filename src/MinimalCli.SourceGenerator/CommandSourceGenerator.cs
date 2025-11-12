using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using MinimalCli.SourceGenerator;
using System.Threading;
using System.Linq;

namespace MinimalCli.SourceGeneration;

[Generator]
public class CommandSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // always generate this code
        //context.RegisterPostInitializationOutput(GenerateMainFunctionCode);

        // attributes
        IncrementalValueProvider<ImmutableArray<GeneratingRootCommandBinder>> rootHandlerProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MinimalCli.RootHandlerAttribute",
                predicate: MethodDeclPredicate,
                transform: GeneratorBindingsProvider.TransformForRoot
            ).Collect();

        IncrementalValueProvider<ImmutableArray<GeneratingCommandBinder>> handlersProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MinimalCli.HandlerAttribute",
                predicate: MethodDeclPredicate,
                transform: GeneratorBindingsProvider.Transform
            ).Collect();

        IncrementalValueProvider<(
            ImmutableArray<GeneratingRootCommandBinder> RootBinders, 
            ImmutableArray<GeneratingCommandBinder> CommandBinders
            )> combined = rootHandlerProvider.Combine(handlersProvider);

        // register output for commands
        context.RegisterSourceOutput(combined, static (spc, binders) => {
            HashSet<string> commandNames = new();

            // first generate and create all of the CommandOptions classes
            foreach (GeneratingCommandBinder? binder in binders.CommandBinders)
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

            if(binders.RootBinders.Length > 1)
            {
                // should only be 1 root handler
                foreach(var root in binders.RootBinders)
                    spc.ReportTooManyRootHandlersError(root.CommandNameLocation);
            }
            else
            {
                GeneratingRootCommandBinder? rootHandler = binders.RootBinders.FirstOrDefault();
                if(rootHandler is not null)
                {
                    // generate root command options
                    string? rootCode = CommandOptionsWriter.GenerateRootCommandOptions(rootHandler);
                    if (rootCode is not null) 
                    {
                        spc.AddSource("RootCommand.g.cs", rootCode);
                    }
                }
            }

            // Emit the aggregated Register method
            string? registryCode = MapAllCommandsExtensionWriter.GenerateMapAllCommandsExt(binders.CommandBinders, binders.RootBinders);
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
    //        using MinimalCli;

    //        namespace MinimalCli;

    //        public static partial class RegisterCommandsHook
    //        {
    //            public static partial void Register(MinimalCommandLineBuilder builder);
    //        }
    //        """
    //        );
    //}
}
