using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void GenerateDbReaders(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        Mapping[] mappings
    )
    {
        WritePartialMappersClassStart(mappersClass, sourceWriter, true);

        sourceWriter.Write("public ");
        sourceWriter.Write(mappersClass.Symbol.Name);
        sourceWriter.WriteLine("() : this(new DefaultDbNameProvider()) { }");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine(
            mappings.Any(m => m.Technique == MappingTechnique.Custom)
                ? "public required DbReaders Readers { get; init; }"
                : "public DbReaders Readers { get; init; } = new DbReaders(nameProvider);"
        );
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public IObjectReaderProvider ReaderProvider => Readers;");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public IDbNameProvider NameProvider => nameProvider;");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public partial class DbReaders : IObjectReaderProvider {");
        sourceWriter.Indent++;

        sourceWriter.WriteLine("private IObjectReaderProvider? _readerProvider;");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public DbReaders(IDbNameProvider nameProvider) { ");
        sourceWriter.Indent++;
        sourceWriter.WriteLine("NameProvider = nameProvider;");
        foreach (var mapping in mappings.Where(m => m.Technique != MappingTechnique.Custom))
        {
            var type = mapping.Type;
            sourceWriter.Write(GetReaderPropertyName(type));
            sourceWriter.Write(" = new ObjectReader<");
            sourceWriter.Write(type.ToDisplayString());
            if (
                type is
                { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated }
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

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("protected IDbNameProvider NameProvider { get; }");

        sourceWriter.WriteLine("public ImmutableArray<IObjectReader> GetAllReaders() => [");
        sourceWriter.Indent++;
        sourceWriter.WriteLine(".. CreateAnonymousReaders(),");

        for (var index = 0; index < mappings.Length; index++)
        {
            var type = mappings[index].Type;
            sourceWriter.Write(GetReaderPropertyName(type));
            if (type.IsValueType)
            {
                sourceWriter.WriteLine(",");
                sourceWriter.Write(GetNullableReaderPropertyName(type));
            }

            if (index < mappings.Length - 1)
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

        sourceWriter.WriteLine("public IObjectReader Get(Type type)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
        sourceWriter.WriteLine(
            "_readerProvider ??= DictionaryObjectReaderProvider.From(GetAllReaders());"
        );
        sourceWriter.WriteLine("return _readerProvider.Get(type);");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");

        WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
    }
}
