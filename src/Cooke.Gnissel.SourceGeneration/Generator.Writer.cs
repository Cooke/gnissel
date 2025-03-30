using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void GenerateWriteMappers(
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
                        },
                (context, ct) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;

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
                        .ToArray();
                }
            )
            .SelectMany((x, _) => x);

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
                WritePartialMappersClassStart(mappersClass, sourceWriter);

                sourceWriter.WriteLine(
                    "public DbWriters Writers { get; init; } = new DbWriters();"
                );
                sourceWriter.WriteLine();

                sourceWriter.WriteLine(
                    "IObjectWriterProvider IMapperProvider.WriterProvider => Writers;"
                );
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public partial class DbWriters : IObjectWriterProvider {");
                sourceWriter.Indent++;

                sourceWriter.WriteLine("private IObjectWriterProvider? _writerProvider;");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public DbWriters() {");
                sourceWriter.Indent++;

                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write(GetWriterPropertyName(type));
                    sourceWriter.Write(" = new ObjectWriter<");
                    sourceWriter.Write(type.ToDisplayString());
                    sourceWriter.Write(">(");
                    sourceWriter.Write(GetWriteMethodName(type));
                    sourceWriter.WriteLine(");");

                    if (type.IsValueType)
                    {
                        sourceWriter.Write(GetNullableWriterPropertyName(type));
                        sourceWriter.Write(" = new ObjectWriter<");
                        sourceWriter.Write(type.ToDisplayString());
                        sourceWriter.Write("?>(");
                        sourceWriter.Write(GetNullableWriteMethodName(type));
                        sourceWriter.WriteLine(");");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public ImmutableArray<IObjectWriter> GetAllWriters() => [");
                sourceWriter.Indent++;

                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write(GetWriterPropertyName(type));
                    if (type.IsValueType)
                    {
                        sourceWriter.WriteLine(",");
                        sourceWriter.Write(GetNullableWriterPropertyName(type));
                    }

                    if (index < types.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public ObjectWriter<TOut> Get<TOut>()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indent++;
                sourceWriter.WriteLine(
                    "_writerProvider ??= DictionaryObjectWriterProvider.From(GetAllWriters());"
                );
                sourceWriter.WriteLine("return _writerProvider.Get<TOut>();");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");

                GenerateWriteMappersClassEnd(mappersClass, sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.WriteMappers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }

    private static void GenerateWriter(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        GenerateWriteMappersClassStart(mappersClass, sourceWriter);

        GenerateWriteMapperProperty(sourceWriter, type);

        if (type.IsValueType)
        {
            GenerateNullableWriterProperty(sourceWriter, type);
        }

        GenerateWriteMethod(type, mappersClass, sourceWriter);

        if (type.IsValueType)
        {
            WriteNullableWriteMethod(sourceWriter, type, mappersClass);
        }

        GenerateWriteMappersClassEnd(mappersClass, sourceWriter);
    }

    private static void GenerateNullableWriterProperty(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        if (type.DeclaredAccessibility != Accessibility.Public)
        {
            sourceWriter.Write(AccessibilityToString(type.DeclaredAccessibility));
            sourceWriter.Write(" ");
        }

        sourceWriter.Write("ObjectWriter<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("?> ");
        sourceWriter.Write(GetNullableWriterPropertyName(type));
        sourceWriter.WriteLine(" { get; init; }");
        sourceWriter.WriteLine();
    }

    private static void GenerateWriteMapperProperty(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        if (type.DeclaredAccessibility != Accessibility.Public)
        {
            sourceWriter.Write(AccessibilityToString(type.DeclaredAccessibility));
            sourceWriter.Write(" ");
        }

        sourceWriter.Write("ObjectWriter<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("> ");
        sourceWriter.Write(GetWriterPropertyName(type));
        sourceWriter.WriteLine(" { get; init; }");
        sourceWriter.WriteLine();
    }

    private static void GenerateWriteMappersClassStart(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        WritePartialMappersClassStart(mappersClass, sourceWriter);
        sourceWriter.WriteLine("public partial class DbWriters {");
        sourceWriter.Indent++;
    }

    private static void GenerateWriteMappersClassEnd(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        WritePartialMappersClassEnd(mappersClass, sourceWriter);
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static string GetNullableWriterPropertyName(ITypeSymbol type) =>
        $"{GetTypeIdentifierName(type)}NullableWriter";

    private static string GetWriterPropertyName(ITypeSymbol type) =>
        GetWriterPropertyName(GetTypeIdentifierName(type));

    private static string GetWriterPropertyName(string typeIdentifierName) =>
        $"{typeIdentifierName}Writer";

    private static string GetWriteMethodName(ITypeSymbol type) =>
        $"Write{GetTypeIdentifierName(type)}";

    private static string GetNullableWriteMethodName(ITypeSymbol type) =>
        $"Write{GetTypeIdentifierName(type)}Nullable";
}
