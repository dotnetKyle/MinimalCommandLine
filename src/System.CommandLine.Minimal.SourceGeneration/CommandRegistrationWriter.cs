using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace System.CommandLine.Minimal.SourceGeneration
{
    internal static class CommandRegistrationWriter
    {
        /// <summary>
        /// This Generates the MapAllCommands() extension, which automatically registers 
        /// the Command classes with dependency injection and the CommandOptions with 
        /// the builder.
        /// </summary>
        /// <param name="binders">The generated command binding information</param>
        /// <returns></returns>
        internal static string? GenerateRegistrations(ImmutableArray<GeneratingCommandBinder> binders)
        {
            if(binders.Length > 0)
            {
                StringBuilder sb = new();
                sb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
                sb.AppendLine("using System.CommandLine;");
                sb.AppendLine("using System.CommandLine.Minimal;");
                sb.AppendLine();
                sb.AppendLine("namespace System.CommandLine.Minimal;");
                sb.AppendLine();
                sb.AppendLine("public static class RegisterCommandsHook");
                sb.AppendLine("{");
                sb.AppendLine("    /// <summary>Ensure that all of the commands get mapped by calling this function.</summary>");
                sb.AppendLine("    public static MinimalCommandLineBuilder MapAllCommands(this MinimalCommandLineBuilder builder)");
                sb.AppendLine("    {");
                foreach(GeneratingCommandBinder binder in binders)
                {
                    if(binder.CommandName is not null)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"        // register '{binder.CommandNameTitleCase}' Command");
                        sb.AppendLine($"        builder.Services.TryAddTransient<{binder.FullClassName}>();");
                        sb.AppendLine($"        builder.TryRegisterCommandOptions<{binder.CommandOptionsName}>();");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine();
                sb.AppendLine("        return builder;");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("}");

                return sb.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
