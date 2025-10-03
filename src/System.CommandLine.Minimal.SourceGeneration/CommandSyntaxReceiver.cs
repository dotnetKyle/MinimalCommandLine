using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace System.CommandLine.Minimal.SourceGeneration
{
    public class CommandSyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax method &&
                method.AttributeLists.Count > 0)
            {
                CandidateMethods.Add(method);
            }
        }
    }

}
