using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
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
