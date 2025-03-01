#nullable enable
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;
using Robust.Shared.Prototypes;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrototypeAnalyzer : DiagnosticAnalyzer
{
    private const string PrototypeAttributeName = "Prototype";
    private const string PrototypeAttributeNamespace = "Robust.Shared.Prototypes.PrototypeAttribute";

    public static readonly DiagnosticDescriptor PrototypeRedundantTypeRule = new(
        Diagnostics.IdPrototypeRedundantType,
        "Redundant Prototype Type specification",
        "Prototype {0} has explicitly set type \"{1}\" that matches autogenerated value",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Remove the redundant type specification."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [PrototypeRedundantTypeRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzePrototype, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzePrototype(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not ITypeSymbol classSymbol)
            return;

        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != PrototypeAttributeNamespace)
                continue;

            if (attribute.ConstructorArguments[0].Value is not string specifiedName)
                continue;

            var autoName = PrototypeUtility.CalculatePrototypeName(classSymbol.Name);
            if (autoName == specifiedName)
            {
                var location = TryGetPrototypeAttribute(classDeclarationSyntax, out var protoNode) ? protoNode.GetLocation() : classDeclarationSyntax.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(PrototypeRedundantTypeRule,
                    location,
                    classSymbol.Name,
                    specifiedName));
            }
        }
    }

    private static bool TryGetPrototypeAttribute(TypeDeclarationSyntax syntax, [NotNullWhen(true)]out AttributeSyntax? prototypeAttribute)
    {
        foreach (var attributeList in syntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == PrototypeAttributeName)
                {
                    prototypeAttribute = attribute;
                    return true;
                }
            }
        }
        prototypeAttribute = null;
        return false;
    }
}
