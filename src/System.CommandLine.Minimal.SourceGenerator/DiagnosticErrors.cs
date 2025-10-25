using Microsoft.CodeAnalysis;

namespace System.CommandLine.Minimal.SourceGenerator
{
    internal static class DiagnosticErrors
    {
        const string CommandNameCategory = "Command Name";
        static readonly DiagnosticDescriptor CommandNameConflict =
            new DiagnosticDescriptor(
                "MIN0001",
                "Command name conflict",
                "Another Handler attribute already has this command name \"{0}\"",
                CommandNameCategory,
                DiagnosticSeverity.Error,
                true
            );
        static readonly DiagnosticDescriptor CommandNameEmpty = 
            new DiagnosticDescriptor(
                "MIN0002",
                "Command name empty",
                "A command name is null or empty",
                CommandNameCategory,
                DiagnosticSeverity.Error,
                true
            );

        /// <summary>
        /// MIN0002 empty command name.
        /// </summary>
        internal static void ReportCommandNameEmptyError(
            this SourceProductionContext ctx,
            Location? location)
            => ctx.ReportDiagnostic(Diagnostic.Create(CommandNameEmpty, location));

        /// <summary>
        /// MIN0001 duplicate command name.
        /// </summary>
        internal static void ReportCommandNameConflict(
            this SourceProductionContext ctx, 
            Location? location, 
            string commandName)
            => ctx.ReportDiagnostic(Diagnostic.Create(CommandNameConflict, location, commandName));

    }
}
