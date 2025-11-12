using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MinimalCli.SourceGeneration;

internal static class MapAllCommandsExtensionWriter
{
    /// <summary>
    /// This Generates the MapAllCommands() extension, which automatically registers 
    /// the Command classes with dependency injection and the CommandOptions with 
    /// the builder.
    /// </summary>
    /// <param name="commandBinders">The generated command binding information</param>
    /// <returns></returns>
    internal static string? GenerateMapAllCommandsExt(ImmutableArray<GeneratingCommandBinder> commandBinders, ImmutableArray<GeneratingRootCommandBinder> rootCommandBinders)
    {
        if(commandBinders.Length > 0 || rootCommandBinders.Length > 0)
        {
            StringBuilder sb = new(
                """
                using Microsoft.Extensions.DependencyInjection.Extensions;
                using System.CommandLine;
                using MinimalCli;

                namespace MinimalCli;

                public static class MapAllCommandsExtension
                {
                    /// <summary>Ensure that all of the commands get mapped by calling this function.</summary>
                    public static MinimalCommandLineBuilder MapAllCommands(this MinimalCommandLineBuilder builder)
                    {
                """);

            // generate code for root command (there should only be one)
            if(rootCommandBinders.Length == 1)
            {
                GeneratingRootCommandBinder? rootBinder = rootCommandBinders.FirstOrDefault();
                if(rootBinder is not null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        // register Root Command");
                    if (!rootBinder.MethodIsStatic)
                    {
                        sb.AppendLine($"        builder.Services.TryAddTransient<{rootBinder.FullClassName}>();");
                    }
                    sb.AppendLine($"        builder.TryRegisterCommandOptions<{rootBinder.CommandOptionsName}>();");
                    sb.AppendLine();
                }
            }

            // generate code for all commands
            foreach(GeneratingCommandBinder binder in commandBinders)
            {
                if(binder.CommandName is not null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        // register '{binder.CommandNameTitleCase}' Command");
                    if(!binder.MethodIsStatic)
                    {
                        sb.AppendLine($"        builder.Services.TryAddTransient<{binder.FullClassName}>();");
                    }
                    sb.AppendLine($"        builder.TryRegisterCommandOptions<{binder.CommandOptionsName}>();");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("""

                        return builder;
                    }
                }
                """);

            return sb.ToString();
        }
        else
        {
            return null;
        }
    }
}
