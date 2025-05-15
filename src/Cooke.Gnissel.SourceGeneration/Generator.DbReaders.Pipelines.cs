using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private void GenerateDbReaders(
        IncrementalGeneratorInitializationContext initContext,
        IncrementalValuesProvider<MappersClass> mappersPipeline
    )
    {
        GenerateAnonymous(initContext, mappersPipeline);
        GenerateRegular(initContext, mappersPipeline);
    }

    private void GenerateRegular(
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
                                    Name.Identifier.ValueText: "ToArrayAsync"
                                        or "ToListAsync"
                                        or "Query"
                                        or "QuerySingle"
                                        or "QuerySingleOrDefault"
                                        or "Select"
                                },
                            }
                            or PropertyDeclarationSyntax
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
                        case InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax memberAccessExpression
                        } invocation:
                        {
                            var operation = context.SemanticModel.GetOperation(invocation, ct);
                            if (
                                operation
                                is not IInvocationOperation
                                {
                                    TargetMethod.TypeArguments: { Length: 1 } typeArguments
                                }
                            )
                            {
                                return null;
                            }

                            if (
                                context
                                    .SemanticModel.GetTypeInfo(
                                        memberAccessExpression.Expression,
                                        ct
                                    )
                                    .Type
                                is not INamedTypeSymbol objectType
                            )
                            {
                                return null;
                            }

                            if (
                                objectType.Name != "DbContext"
                                && objectType.BaseType?.Name != "DbContext"
                                && objectType.AllInterfaces.All(i => i.Name != "IQuery")
                            )
                            {
                                return null;
                            }

                            var typeArg = typeArguments[0];
                            return typeArg.IsAnonymousType ? null : typeArg;
                        }

                        case PropertyDeclarationSyntax
                        {
                            Type: GenericNameSyntax genericNameSyntax
                        }:
                        {
                            var typeInfo = context.SemanticModel.GetTypeInfo(
                                genericNameSyntax.TypeArgumentList.Arguments[0],
                                ct
                            );
                            return typeInfo.Type ?? null;
                        }

                        default:
                            throw new InvalidOperationException("Unexpected node type");
                    }
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

    private void GenerateAnonymous(
        IncrementalGeneratorInitializationContext initContext,
        IncrementalValuesProvider<MappersClass> mappersPipeline
    )
    {
        var anonymousTypesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node
                        is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name.Identifier.ValueText: "Select",
                                Name: not GenericNameSyntax
                            },
                            ArgumentList.Arguments.Count: 1
                        },
                (context, ct) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var operation = context.SemanticModel.GetOperation(invocation, ct);
                    if (
                        operation
                        is not IInvocationOperation
                        {
                            TargetMethod.TypeArguments: { Length: 1 } typeArguments
                        }
                    )
                    {
                        return null;
                    }

                    var typeArg = typeArguments[0];
                    if (!typeArg.IsAnonymousType)
                    {
                        return null;
                    }

                    return typeArg;
                }
            )
            .Where(x => x != null)
            .Select((v, _) => v!);

        var mapperClassesWithAnonymousPipeline = mappersPipeline
            .Combine(anonymousTypesPipeline.Collect())
            .Select(
                (pair, _) =>
                {
                    var mappersClass = pair.Left;
                    var types = pair.Right;
                    return (
                        mappersClass,
                        types: types
                            .Where(t => IsAccessibleDeep(t, mappersClass))
                            .Select(AdjustNulls)
                            .Distinct(SymbolEqualityComparer.Default)
                            .Cast<ITypeSymbol>()
                            .ToArray()
                    );
                }
            );

        initContext.RegisterImplementationSourceOutput(
            mapperClassesWithAnonymousPipeline,
            (context, readTypeWithMappersClass) =>
            {
                var mappersClass = readTypeWithMappersClass.mappersClass;
                var anonymousTypes = readTypeWithMappersClass.types;

                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);

                GenerateAnonymousReaders(mappersClass, sourceWriter, anonymousTypes);

                sourceWriter.Flush();
                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.Anonymous.cs",
                    stringWriter.ToString()
                );
            }
        );
    }
}
