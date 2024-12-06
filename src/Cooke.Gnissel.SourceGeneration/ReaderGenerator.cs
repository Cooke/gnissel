using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public class ReaderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // initContext.RegisterPostInitializationOutput(ctx =>
        //     ctx.AddSource(
        //         "Test.cs",
        //         SourceText.From("namespace Test { public class Test { } }", Encoding.UTF8)
        //     )
        // );

        var pipeline = initContext.SyntaxProvider.CreateSyntaxProvider(
            (node, _) =>
                node
                    is InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax
                        {
                            Name: { Identifier: { ValueText: "Query" } }
                        }
                    },
            (context, ct) => context.SemanticModel.GetTypeInfo(context.Node, ct)
        );

        initContext.RegisterSourceOutput(
            pipeline,
            (context, type) =>
            {
                var sourceText = SourceText.From(
                    $$"""
                    namespace Test;
                    partial class GeneratedDbReader
                    {
                        partial {{type.Type?}} Read{{type.Type?.Name}}()
                        {
                            return new {{type.Type?.Name}}();
                        }
                    }
                    """,
                    Encoding.UTF8
                );

                context.AddSource($"GeneratedDbReader_{type.Type?.Name}.cs", sourceText);
            }
        );
    }
}
