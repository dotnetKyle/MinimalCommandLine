using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace System.CommandLine.Minimal.SourceGeneration
{
    internal static class MapAllCommandsExtensionWriter
    {
        /// <summary>
        /// This Generates the MapAllCommands() extension, which automatically registers 
        /// the Command classes with dependency injection and the CommandOptions with 
        /// the builder.
        /// </summary>
        /// <param name="binders">The generated command binding information</param>
        /// <returns></returns>
        internal static string? GenerateMapAllCommandsExt(ImmutableArray<GeneratingCommandBinder> binders)
        {
            if(binders.Length > 0)
            {
                StringBuilder sb = new(
                    """
                    using Microsoft.Extensions.DependencyInjection.Extensions;
                    using System.CommandLine;
                    using System.CommandLine.Minimal;

                    namespace System.CommandLine.Minimal;

                    public static class MapAllCommandsExtension
                    {
                        /// <summary>Ensure that all of the commands get mapped by calling this function.</summary>
                        public static MinimalCommandLineBuilder MapAllCommands(this MinimalCommandLineBuilder builder)
                        {
                    """);
                foreach(GeneratingCommandBinder binder in binders)
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
}
