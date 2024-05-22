#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentAnalyzer : DiagnosticAnalyzer
{
    private const string RegisterComponentNamespace = "Robust.Shared.GameObjects.RegisterComponentAttribute";

    private const string IgnoredNamespaceFilter = "Robust";

    private static readonly DiagnosticDescriptor ComponentUndocumentedRule = new(
        Diagnostics.IdComponentUndocumented,
        "Component undocumented",
        "Component {0} needs XML documentation",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Make sure to document the component with an XML comment."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ComponentUndocumentedRule
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(CheckDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void CheckDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not TypeDeclarationSyntax node)
            return;

        // Make sure it's public
        if (!node.Modifiers.Any(SyntaxKind.PublicKeyword))
            return;

        var type = context.SemanticModel.GetDeclaredSymbol(node)!;
        // Engine components get a pass
        if (type.ContainingNamespace.ToString().Contains(IgnoredNamespaceFilter))
            return;

        // If it's not a registered component, it's none of our business
        if (!IsRegisteredComponent(type))
            return;

        var comment = node.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        // No comment?
        if (comment == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(ComponentUndocumentedRule, node.Identifier.GetLocation(), type.Name));
        }
    }

    private bool IsRegisteredComponent(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == RegisterComponentNamespace)
                return true;
        }

        return false;
    }
}
