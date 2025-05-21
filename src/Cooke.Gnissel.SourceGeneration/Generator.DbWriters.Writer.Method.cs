using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void WriteNullableWriteMethod(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type,
        MappersClass mappersClass
    )
    {
        sourceWriter.Write("private void ");
        sourceWriter.Write(GetNullableWriteMethodName(type));
        sourceWriter.Write("(");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.WriteLine("? value, IParameterWriter parameterWriter) {");
        sourceWriter.Indent++;

        GenerateWriteMethodBody(type, mappersClass, sourceWriter);

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void GenerateWriteMethod(
        ITypeSymbol type,
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        sourceWriter.Write("private void ");
        sourceWriter.Write(GetWriteMethodName(type));
        sourceWriter.Write("(");
        sourceWriter.Write(
            type.IsReferenceType ? type.ToNullableDisplayString() : type.ToDisplayString()
        );
        sourceWriter.WriteLine(" value, IParameterWriter parameterWriter) {");
        sourceWriter.Indent++;

        if (type.IsValueType)
        {
            sourceWriter.Write(GetNullableWriterPropertyName(type));
            sourceWriter.WriteLine(".Write(value, parameterWriter);");
        }
        else
        {
            GenerateWriteMethodBody(type, mappersClass, sourceWriter);
        }

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();
    }

    private static void GenerateWriteMethodBody(
        ITypeSymbol type,
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        if (IsBuildIn(type))
        {
            sourceWriter.Write("parameterWriter.Write(value");
            sourceWriter.WriteLine(");");
        }
        else if (
            CreateMapping(type) is { Technique: MappingTechnique.AsIs, DbDataType: var dbDataType }
        )
        {
            sourceWriter.Write("parameterWriter.Write(value");
            sourceWriter.Write(dbDataType is not null ? $", \"{dbDataType}\"" : string.Empty);
            sourceWriter.WriteLine(");");
        }
        else if (
            type is INamedTypeSymbol { EnumUnderlyingType: not null and var underlyingEnumType }
        )
        {
            switch (mappersClass.EnumMappingTechnique)
            {
                case MappingTechnique.AsIs:
                    sourceWriter.WriteLine("parameterWriter.Write(value);");
                    break;

                case MappingTechnique.AsString:
                    sourceWriter.WriteLine("parameterWriter.Write(value.ToString());");
                    break;

                case MappingTechnique.AsInteger:
                    sourceWriter.WriteLine("parameterWriter.Write((int)value);");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            var props = GetWriteProperties(type);

            foreach (var prop in props)
            {
                sourceWriter.Write(
                    prop.Type.IsValueType && !IsNullableValueType(prop.Type)
                        ? GetNullableWriterPropertyName(prop.Type)
                        : GetWriterPropertyName(prop.Type)
                );
                sourceWriter.Write(".Write(value?.");
                sourceWriter.Write(prop.Name);
                sourceWriter.WriteLine(", parameterWriter);");
            }
        }
    }
}
