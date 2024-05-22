
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<Robust.Analyzers.ComponentAnalyzer>;

namespace Robust.Analyzers.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
public sealed class ComponentAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ComponentAnalyzer, NUnitVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using System;
            using Robust.Shared.GameObjects;

            namespace Robust.Shared.GameObjects
            {
                public interface Component;
                public sealed class RegisterComponentAttribute : Attribute;
            }

            /// <summary>
            /// Good is a good component, yes it is.
            /// </summary>
            [RegisterComponent]
            public sealed partial class Good : Component
            {
            }

            [RegisterComponent]
            public sealed partial class Bad : Component
            {
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(11,29): warning RA0028: Component Foo needs XML documentation
            VerifyCS.Diagnostic().WithSpan(19, 29, 19, 32).WithArguments("Bad")
        );
    }
}
