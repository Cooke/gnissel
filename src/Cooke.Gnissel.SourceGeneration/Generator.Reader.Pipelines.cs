using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private void GenerateReadMappers(
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
                (context, ct) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                    var instanceType = context.SemanticModel.GetTypeInfo(
                        memberAccess.Expression,
                        ct
                    );

                    // TODO check that instance type is a DbContext

                    var genericName = (GenericNameSyntax)memberAccess.Name;
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    var typeArgInfo = context.SemanticModel.GetTypeInfo(typeArg);
                    if (typeArgInfo.Type is null)
                    {
                        return null;
                    }

                    return typeArgInfo.Type;
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
                WritePartialMappersClassStart(mappersClass, sourceWriter);

                sourceWriter.WriteLine(
                    types.Any(NeedCustomReader)
                        ? "public required DbReaders Readers { get; init; }"
                        : "public DbReaders Readers { get; init; } = new DbReaders();"
                );
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public IObjectReaderProvider ReaderProvider => Readers;");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public partial class DbReaders : IObjectReaderProvider {");
                sourceWriter.Indent++;

                sourceWriter.WriteLine("private IObjectReaderProvider? _readerProvider;");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public DbReaders() { ");
                sourceWriter.Indent++;
                foreach (var type in types)
                {
                    if (!NeedCustomReader(type))
                    {
                        sourceWriter.Write(GetReaderPropertyName(type));
                        sourceWriter.Write(" = new ObjectReader<");
                        sourceWriter.Write(type.ToDisplayString());
                        if (
                            type is
                            {
                                IsReferenceType: true,
                                NullableAnnotation: NullableAnnotation.NotAnnotated
                            }
                        )
                        {
                            sourceWriter.Write("?");
                        }

                        sourceWriter.Write(">(");
                        sourceWriter.Write(GetReadMethodName(type));
                        sourceWriter.Write(",");
                        sourceWriter.Write(GetCreateReaderDescriptorsName(type));
                        sourceWriter.WriteLine(");");

                        if (type.IsValueType)
                        {
                            sourceWriter.Write(GetNullableReaderPropertyName(type));
                            sourceWriter.Write(" = new ObjectReader<");
                            sourceWriter.Write(type.ToDisplayString());
                            sourceWriter.Write("?>(");
                            sourceWriter.Write(GetNullableReadMethodName(type));
                            sourceWriter.Write(",");
                            sourceWriter.Write(GetCreateReaderDescriptorsName(type));
                            sourceWriter.WriteLine(");");
                        }
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public ImmutableArray<IObjectReader> GetAllReaders() => [");
                sourceWriter.Indent++;

                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write(GetReaderPropertyName(type));
                    if (type.IsValueType)
                    {
                        sourceWriter.WriteLine(",");
                        sourceWriter.Write(GetNullableReaderPropertyName(type));
                    }

                    if (index < types.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public ObjectReader<TOut> Get<TOut>()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indent++;
                sourceWriter.WriteLine(
                    "_readerProvider ??= DictionaryObjectReaderProvider.From(GetAllReaders());"
                );
                sourceWriter.WriteLine("return _readerProvider.Get<TOut>();");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");

                WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }
}
