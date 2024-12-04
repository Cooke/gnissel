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
        initContext.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(
                "Test.cs",
                SourceText.From("namespace Test { public class Test { } }", Encoding.UTF8)
            )
        );

        var pipeline = initContext.SyntaxProvider.ForAttributeWithMetadataName(
            "Cooke.Gnissel.DbReadAttribute",
            (node, _) => node is TypeDeclarationSyntax,
            (context, _) =>
                context.TargetSymbol as ITypeSymbol ?? throw new Exception("Expected a type symbol")
        );

        //         initContext.RegisterSourceOutput(
        //             pipeline,
        //             (context, type) =>
        //             {
        //                 var sourceText = SourceText.From(
        //                     $$"""
        //                     namespace Test;
        //                     partial class GeneratedDbReader
        //                     {
        //                         partial {{type.Name}} Read{{type.Name}}()
        //                         {
        //                             return new {{type.Name}}();
        //                         }
        //                     }
        //                     """,
        //                     Encoding.UTF8
        //                 );
        //
        //                 context.AddSource($"GeneratedDbReader_{type.Name}.cs", sourceText);
        //             }
        //         );
    }
}
