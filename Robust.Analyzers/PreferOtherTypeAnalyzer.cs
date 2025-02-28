#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferOtherTypeAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeType = "Robust.Shared.Analyzers.PreferOtherTypeAttribute";
    public static readonly string ReplacementType = "replacement";

    private static readonly DiagnosticDescriptor PreferOtherTypeDescriptor = new(
        Diagnostics.IdPreferOtherType,
        "Use the specific type",
        "Use the specific type {0} instead of {1} when the type argument is {2}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Use the specific type.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        PreferOtherTypeDescriptor
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics | GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeGeneric, SyntaxKind.GenericName);
    }

    private void AnalyzeGeneric(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not GenericNameSyntax genericName)
            return;
        CheckType(genericName, context);
    }

    private void CheckType(TypeSyntax syntax, SyntaxNodeAnalysisContext context)
    {
        var preferOtherTypeAttribute = context.Compilation.GetTypeByMetadataName(AttributeType);

        // Get the type of the generic being used
        if (syntax is not GenericNameSyntax genericName)
            return;
        var genericSyntax = genericName.TypeArgumentList.Arguments[0];

        if (context.SemanticModel.GetSymbolInfo(genericSyntax).Symbol is not { } symbol)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(syntax);
        if (symbolInfo.Symbol?.GetAttributes() is not { } attributes)
            return;

        foreach (var attribute in attributes)
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, preferOtherTypeAttribute))
                continue;

            // See if the generic type argument matches the type the attribute specifies
            if (attribute.ConstructorArguments[0].Value is not ITypeSymbol checkedType)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(checkedType, symbol))
                continue;
            if (attribute.ConstructorArguments[1].Value is not ITypeSymbol replacementType)
                continue;
            var props = new Dictionary<string, string?>
            {
                { ReplacementType, replacementType.Name }
            };
            context.ReportDiagnostic(Diagnostic.Create(PreferOtherTypeDescriptor,
                syntax.GetLocation(),
                props.ToImmutableDictionary(),
                replacementType.Name,
                symbolInfo.Symbol.Name,
                symbol.Name));
        }
    }
}
