using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void GenerateWriterBody(
        ITypeSymbol type,
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        if (IsBuildIn(type))
        {
            sourceWriter.WriteLine("parameterWriter.Write(value);");
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
            sourceWriter.WriteLine("parameterWriter.Write(value);");
        }
    }
}
