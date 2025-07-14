using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrototypeCacheAnalyzer : DiagnosticAnalyzer
{
    private const string PrototypeInterfaceType = "Robust.Shared.Prototypes.IPrototype";
    private const string ComponentInterfaceType = "Robust.Shared.GameObjects.IComponent";
    private const string EntitySystemInterfaceType = "Robust.Shared.GameObjects.IEntitySystem";
    private const string MetaDataComponentType = "Robust.Shared.GameObjects.MetaDataComponent";

    private static readonly string[] SearchedTypes =
    [
        ComponentInterfaceType,
        EntitySystemInterfaceType
    ];

    public static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdPrototypeCached,
        "Cached prototype found",
        "Do not cache prototypes. Use ProtoIds and index them from the prototype manager.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Rule,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(ctx =>
        {
            if (ctx.Compilation.GetTypeByMetadataName(PrototypeInterfaceType) is not { } prototypeInterface)
                return;

            List<INamedTypeSymbol> searchedTypes = [];
            foreach (var searchedType in SearchedTypes)
            {
                if (ctx.Compilation.GetTypeByMetadataName(searchedType) is { } typeSymbol)
                    searchedTypes.Add(typeSymbol);
            }

            if (ctx.Compilation.GetTypeByMetadataName(MetaDataComponentType) is not { } metaDataComponent)
                return;

            ctx.RegisterSymbolStartAction(symContext =>
            {
                if (symContext.Symbol is not INamedTypeSymbol namedTypeSymbol)
                    return;

                if (namedTypeSymbol.TypeKind != TypeKind.Class || namedTypeSymbol.IsStatic)
                    return;

                // MetaDataComponent gets a special exemption from this rule
                if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, metaDataComponent))
                    return;

                // We only care about certain types
                if (!searchedTypes.Any(type => TypeSymbolHelper.ImplementsInterface(namedTypeSymbol, type)))
                    return;

                // Analyze the fields of this class
                symContext.RegisterSyntaxNodeAction(synContext => AnalyzeField(synContext, prototypeInterface), SyntaxKind.FieldDeclaration);

            }, SymbolKind.NamedType);
        });
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context, INamedTypeSymbol prototypeInterface)
    {
        if (context.Node is not FieldDeclarationSyntax node)
            return;

        // Get the field's type
        if (context.SemanticModel.GetTypeInfo(node.Declaration.Type).Type is not { } fieldType)
            return;

        // Check if the field's type is a prototype
        if (TypeSymbolHelper.ImplementsInterface(fieldType, prototypeInterface))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule,
                node.Declaration.Type.GetLocation()
            ));
        }
    }
}
