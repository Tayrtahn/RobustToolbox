using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.PrototypeCacheAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(PrototypeCacheAnalyzer))]
public sealed class PrototypeCacheAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new RTAnalyzerTest<PrototypeCacheAnalyzer>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Prototypes.Attributes.cs",
            "Robust.Shared.Prototypes.IPrototype.cs",
            "Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute.cs"
        );

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using Robust.Shared.Prototypes;
        using Robust.Shared.Serialization;

        namespace Robust.Shared.GameObjects
        {
            public interface IComponent;
            public interface IEntitySystem;
            public sealed class MetaDataComponent : IComponent;
        }

        [Prototype]
        public sealed class FooPrototype : IPrototype
        {
            [IdDataField]
            public string ID { get; private set; } = default!;
        }
    """;

    [Test]
    public async Task TestComponent()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            public sealed class Tester : IComponent
            {
                private FooPrototype _cachedFoo;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,13): warning RA0040: Do not cache prototypes. Use ProtoIds and index them from the prototype manager.
            VerifyCS.Diagnostic().WithSpan(5, 13, 5, 25)
        );
    }

    [Test]
    public async Task TestEntitySystem()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            public sealed class Tester : IEntitySystem
            {
                private FooPrototype _cachedFoo;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,13): warning RA0040: Do not cache prototypes. Use ProtoIds and index them from the prototype manager.
            VerifyCS.Diagnostic().WithSpan(5, 13, 5, 25)
        );
    }
}
