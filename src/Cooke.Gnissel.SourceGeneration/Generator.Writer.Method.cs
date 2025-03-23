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
            var props = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic)
                .ToArray();

            if (type.IsReferenceType)
            {
                sourceWriter.WriteLine("if (value is null)");
                sourceWriter.WriteLine("{");
                sourceWriter.Indent++;
                foreach (var prop in props)
                {
                    sourceWriter.Write("parameterWriter.Write<");
                    sourceWriter.Write(prop.Type.ToDisplayString());
                    sourceWriter.WriteLine("?>(null);");
                }
                sourceWriter.WriteLine("return;");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();
            }

            foreach (var prop in props)
            {
                sourceWriter.Write("parameterWriter.Write(value.");
                sourceWriter.Write(prop.Name);
                sourceWriter.WriteLine(");");
            }
        }
    }
}
