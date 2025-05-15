using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void GenerateDbWriters(
        IncrementalGeneratorInitializationContext initContext,
        IncrementalValuesProvider<MappersClass> mappersPipeline
    )
    {
        var queryWriteTypesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (x, _) =>
                    x
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
                                        or { Identifier.ValueText: "NonQuery" }
                                }
                            }
                            or PropertyDeclarationSyntax
                            {
                                Type: GenericNameSyntax
                                {
                                    Identifier.ValueText: "Table",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            }
                            or ObjectCreationExpressionSyntax
                            {
                                Type: GenericNameSyntax
                                {
                                    Identifier.ValueText: "Table",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            },
                (context, ct) =>
                {
                    switch (context.Node)
                    {
                        case PropertyDeclarationSyntax
                        {
                            Type: GenericNameSyntax genericNameSyntax
                        }:
                        {
                            var typeInfo = context.SemanticModel.GetTypeInfo(
                                genericNameSyntax.TypeArgumentList.Arguments[0]
                            );
                            return typeInfo.Type is null ? [] : [typeInfo.Type];
                        }

                        case ObjectCreationExpressionSyntax
                        {
                            Type: GenericNameSyntax genericNameSyntax
                        }:
                        {
                            var typeInfo = context.SemanticModel.GetTypeInfo(
                                genericNameSyntax.TypeArgumentList.Arguments[0]
                            );
                            return typeInfo.Type is null ? [] : [typeInfo.Type];
                        }

                        case InvocationExpressionSyntax invocation:
                        {
                            if (
                                invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression
                                is not InterpolatedStringExpressionSyntax interpolatedString
                            )
                            {
                                return [];
                            }

                            return interpolatedString
                                .Contents.OfType<InterpolationSyntax>()
                                .Select(x => context.SemanticModel.GetTypeInfo(x.Expression))
                                .Where(writeTypeInfo => writeTypeInfo.Type != null)
                                .Select(x => x.Type!)
                                .Where(type =>
                                    type.ContainingType?.Name != "Sql" && type.Name != "Raw"
                                )
                                .ToArray();
                        }
                        default:
                            throw new InvalidOperationException("Unexpected node type");
                    }
                }
            )
            .SelectMany((x, _) => x)
            .Where(x => x.Name != "Object");

        var mappersClassWithQueryWriteTypesPipeline = mappersPipeline
            .Combine(queryWriteTypesPipeline.Collect())
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
            mappersClassWithQueryWriteTypesPipeline.SelectMany(
                (x, _) => x.types.Select(t => (type: t, x.mappersClass))
            ),
            (context, mappersClassWithWriteType) =>
            {
                var mappersClass = mappersClassWithWriteType.mappersClass;
                var type = mappersClassWithWriteType.type;
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);

                GenerateWriter(mappersClass, sourceWriter, type);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.WriteMappers.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        initContext.RegisterImplementationSourceOutput(
            mappersClassWithQueryWriteTypesPipeline,
            (context, mapperWithTypes) =>
            {
                var types = mapperWithTypes.types;
                var mappersClass = mapperWithTypes.mappersClass;
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                GenerateDbWriters(mappersClass, sourceWriter, types);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.WriteMappers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }
}
