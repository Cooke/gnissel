using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void GenerateWriteMappersClassStart(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        WritePartialMappersClassStart(mappersClass, sourceWriter);
        sourceWriter.WriteLine("public static partial class WriteMappers {");
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
                            .SelectMany(FindAllReadTypes)
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

                GenerateWriteMappersClassStart(mappersClass, sourceWriter);
                sourceWriter.WriteLine();
                //GenerateWriteMapperMetadata(sourceWriter, type);
                GenerateWriteMapperDescriptorField(sourceWriter, type);

                // if (type.IsValueType)
                // {
                //     WriteNotNullableObjectReaderDescriptorField(sourceWriter, type);
                //     WriteNotNullableReadMethod(sourceWriter, type);
                // }

                GenerateCreateWriteMapperMethodStart(sourceWriter, type);
                GenerateWriterBody(type, mappersClass, sourceWriter);
                GenerateCreateWriteMapperMethodEnd(sourceWriter);
                GenerateWriteMappersClassEnd(mappersClass, sourceWriter);
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
                GenerateWriteMappersClassStart(mappersClass, sourceWriter);

                sourceWriter.WriteLine(
                    "public static ImmutableArray<IObjectWriterDescriptor> GetWriteDescriptors() => ["
                );
                sourceWriter.Indent++;

                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write("WriteMappers.");
                    sourceWriter.Write(GetWriteMapperDescriptorFieldName(type));
                    // if (type.IsValueType)
                    // {
                    //     sourceWriter.WriteLine(",");
                    //     sourceWriter.Write("WriteMappers.");
                    //     sourceWriter.Write(GetNotNullableWritDescriptorFieldName(type));
                    // }

                    if (index < types.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");

                GenerateWriteMappersClassEnd(mappersClass, sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.WriteMappers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }

    private static void GenerateCreateWriteMapperMethodEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("};");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();
    }

    private static void GenerateWriteMapperDescriptorField(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("public static readonly WriteMapperDescriptor<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write("> ");
        sourceWriter.Write(GetWriteMapperDescriptorFieldName(type));
        sourceWriter.Write(" = new(");
        sourceWriter.Write(GetCreateWriteMapperMethodName(type));
        // sourceWriter.Write(", ");
        // sourceWriter.Write(GetReaderMetadataName(type));
        sourceWriter.WriteLine(");");
        sourceWriter.WriteLine();
    }

    private static void GenerateCreateWriteMapperMethodStart(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("private static ObjectWriterFunc<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write("> ");
        sourceWriter.Write(GetCreateWriteMapperMethodName(type));
        sourceWriter.WriteLine("(ObjectWriterCreateContext context)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;

        // if (!IsBuildIn(type))
        // {
        //     var ctor = GetCtor(type);
        //     var typeSymbols = ctor
        //         .Parameters.Select(x => x.Type)
        //         .Where(x => !BuildInDirectlyMappedTypes.Contains(x.Name))
        //         .Distinct(SymbolEqualityComparer.Default)
        //         .OfType<ITypeSymbol>()
        //         .ToArray();
        //     foreach (var usedType in typeSymbols)
        //     {
        //         sourceWriter.Write("var ");
        //         sourceWriter.Write(GetWriterVariableName(usedType));
        //         sourceWriter.Write(" = context.ReaderProvider.Get<");
        //         WriteTypeNameEnsureNullable(sourceWriter, usedType);
        //         sourceWriter.WriteLine(">();");
        //     }
        //
        //     if (typeSymbols.Any())
        //     {
        //         sourceWriter.WriteLine();
        //     }
        // }

        sourceWriter.WriteLine("return (value, parameterWriter) =>");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private static string GetWriterVariableName(ITypeSymbol usedType)
    {
        var typeIdentifierName = GetTypeIdentifierName(usedType);
        return char.ToLower(typeIdentifierName[0]) + typeIdentifierName.Substring(1) + "Writer";
    }

    private static string GetWriteMapperDescriptorFieldName(ITypeSymbol type) =>
        GetWriteMapperDescriptorFieldName(GetTypeIdentifierName(type));

    private static string GetWriteMapperDescriptorFieldName(string typeIdentifierName) =>
        $"{typeIdentifierName}WriteMapperDescriptor";

    private static string GetCreateWriteMapperMethodName(ITypeSymbol type) =>
        $"Create{GetTypeIdentifierName(type)}WriteFunc";
}
