using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private void GenerateDbReaders(
        IncrementalGeneratorInitializationContext initContext,
        IncrementalValuesProvider<MappersClass> mappersPipeline
    )
    {
        var queryTypesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node
                        is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name: GenericNameSyntax
                                {
                                    Identifier.ValueText: "Query"
                                        or "QuerySingle"
                                        or "QuerySingleOrDefault",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            }
                        },
                (context, _) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                    var genericName = (GenericNameSyntax)memberAccess.Name;
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    var typeArgInfo = context.SemanticModel.GetTypeInfo(typeArg);
                    return typeArgInfo.Type ?? null;
                }
            )
            .Where(type => type != null)
            .Select((input, _) => input!)
            // Currently indirect usage is not supported (unbound type parameters)
            .Where(type => type is not ITypeParameterSymbol)
            .Collect();

        var mapperClassesWithTypesPipeline = mappersPipeline
            .Combine(queryTypesPipeline)
            .Select(
                (pair, _) =>
                {
                    var mappersClass = pair.Left;
                    var types = pair.Right;
                    return (
                        mappersClass,
                        types: types
                            .Where(t => IsAccessibleDeep(t, mappersClass))
                            .SelectMany(FindAllTypes)
                            .Select(AdjustNulls)
                            .Distinct(SymbolEqualityComparer.Default)
                            .Cast<ITypeSymbol>()
                            .ToArray()
                    );
                }
            );

        initContext.RegisterImplementationSourceOutput(
            mapperClassesWithTypesPipeline.SelectMany(
                (mappersClassWithTypes, _) =>
                    mappersClassWithTypes.types.Select(t => new ReadTypeWithMappersClass(
                        t,
                        mappersClassWithTypes.mappersClass
                    ))
            ),
            (context, readTypeWithMappersClass) =>
            {
                var type = readTypeWithMappersClass.Type;
                var mappersClass = readTypeWithMappersClass.MappersClass;

                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);

                GenerateReader(mappersClass, sourceWriter, type);

                sourceWriter.Flush();
                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        initContext.RegisterImplementationSourceOutput(
            mapperClassesWithTypesPipeline,
            (context, mapperWithTypes) =>
            {
                var types = mapperWithTypes.types;
                var mappersClass = mapperWithTypes.mappersClass;
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);

                GenerateDbReaders(mappersClass, sourceWriter, types);

                sourceWriter.Flush();
                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }
}
