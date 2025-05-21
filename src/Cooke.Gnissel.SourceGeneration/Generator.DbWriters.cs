using System.CodeDom.Compiler;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void GenerateDbWriters(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        Mapping[] mappings
    )
    {
        WritePartialMappersClassStart(mappersClass, sourceWriter);

        sourceWriter.WriteLine(
            "public DbWriters Writers { get; init; } = new DbWriters(nameProvider);"
        );
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("IObjectWriterProvider IMapperProvider.WriterProvider => Writers;");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public partial class DbWriters : IObjectWriterProvider {");
        sourceWriter.Indent++;

        sourceWriter.WriteLine("private IObjectWriterProvider? _writerProvider;");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public DbWriters(IDbNameProvider nameProvider) {");
        sourceWriter.Indent++;
        sourceWriter.WriteLine("NameProvider = nameProvider;");

        for (var index = 0; index < mappings.Length; index++)
        {
            var type = mappings[index].Type;
            sourceWriter.Write(GetWriterPropertyName(type));
            sourceWriter.Write(" = new ObjectWriter<");
            sourceWriter.Write(type.ToDisplayString());
            sourceWriter.Write(">(");
            sourceWriter.Write(GetWriteMethodName(type));
            sourceWriter.Write(", ");
            sourceWriter.Write(GetCreateWriterDescriptorsName(type));
            sourceWriter.WriteLine(");");

            if (type.IsValueType)
            {
                sourceWriter.Write(GetNullableWriterPropertyName(type));
                sourceWriter.Write(" = new ObjectWriter<");
                sourceWriter.Write(type.ToDisplayString());
                sourceWriter.Write("?>(");
                sourceWriter.Write(GetNullableWriteMethodName(type));
                sourceWriter.Write(", ");
                sourceWriter.Write(GetCreateWriterDescriptorsName(type));
                sourceWriter.WriteLine(");");
            }
        }

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("protected IDbNameProvider NameProvider { get; }");

        sourceWriter.WriteLine("public ImmutableArray<IObjectWriter> GetAllWriters() => [");
        sourceWriter.Indent++;

        for (var index = 0; index < mappings.Length; index++)
        {
            var type = mappings[index].Type;
            sourceWriter.Write(GetWriterPropertyName(type));
            if (type.IsValueType)
            {
                sourceWriter.WriteLine(",");
                sourceWriter.Write(GetNullableWriterPropertyName(type));
            }

            if (index < mappings.Length - 1)
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

        sourceWriter.WriteLine("public IObjectWriter Get(Type type)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
        sourceWriter.WriteLine(
            "_writerProvider ??= DictionaryObjectWriterProvider.From(GetAllWriters());"
        );
        sourceWriter.WriteLine("return _writerProvider.Get(type);");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");

        GenerateWriteMappersClassEnd(mappersClass, sourceWriter);
    }
}
