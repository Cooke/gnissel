using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private void GenerateAnonymousReaders(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        ITypeSymbol[] types
    )
    {
        WritePartialReadMappersClassStart(mappersClass, sourceWriter);
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("public IEnumerable<IObjectReader> CreateAnonymousReaders() => [");
        sourceWriter.Indent++;

        for (var index = 0; index < types.Length; index++)
        {
            var type = types[index];
            sourceWriter.WriteLine("IObjectReader.Create((reader, ordinalReader) => {");
            sourceWriter.Indent++;

            GenerateAnonymousReadMethodBody(type, sourceWriter);

            sourceWriter.Indent--;
            sourceWriter.WriteLine("},");

            sourceWriter.WriteLine("() => ");
            GenerateAnonymousMetadata(type, sourceWriter, mappersClass);
            sourceWriter.WriteLine(")");

            if (index < types.Length - 1)
            {
                sourceWriter.WriteLine(",");
            }
        }

        sourceWriter.WriteLine("];");

        WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
    }

    private void GenerateAnonymousMetadata(
        ITypeSymbol type,
        IndentedTextWriter sourceWriter,
        MappersClass mappersClass
    )
    {
        sourceWriter.WriteLine("[");
        sourceWriter.Indent++;
        var props = type.GetMembers().OfType<IPropertySymbol>().ToArray();

        for (int i = 0; i < props.Length; i++)
        {
            sourceWriter.Write("..");
            sourceWriter.Write(GetReaderPropertyName(props[i].Type));
            sourceWriter.Write(".ReadDescriptors.Select(d => d.WithParent(");
            sourceWriter.WriteStringOrNull(props[i].Name);
            sourceWriter.Write("))");
            if (i < props.Length - 1)
            {
                sourceWriter.WriteLine(",");
            }
        }

        sourceWriter.Indent--;
        sourceWriter.WriteLine("]");
    }

    private void GenerateAnonymousReadMethodBody(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        var props = type.GetMembers().OfType<IPropertySymbol>().ToArray();

        foreach (var property in props)
        {
            sourceWriter.Write("var ");
            sourceWriter.Write(property.Name);
            sourceWriter.Write(" = ");
            GenerateReadCall(property.Type, sourceWriter);
            sourceWriter.WriteLine(";");
        }
        sourceWriter.WriteLine();

        sourceWriter.Write("if (");
        for (var i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            sourceWriter.Write(prop.Name);
            sourceWriter.Write(" is null");

            if (i < props.Length - 1)
            {
                sourceWriter.Write(" && ");
            }
        }

        sourceWriter.WriteLine(")");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
        sourceWriter.WriteLine("return null;");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();

        sourceWriter.WriteLine("return new {");
        sourceWriter.Indent++;
        for (var index = 0; index < props.Length; index++)
        {
            var property = props[index];
            sourceWriter.Write(property.Name);
            sourceWriter.Write(" = ");
            sourceWriter.Write(property.Name);

            if (
                property.Type is
                { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated }
            )
            {
                sourceWriter.Write(
                    " ?? throw new InvalidOperationException(\"Expected non-null value\")"
                );
            }
            else if (property.Type.IsValueType && !IsNullableValueType(property.Type))
            {
                sourceWriter.Write(
                    " ?? throw new InvalidOperationException(\"Expected non-null value\")"
                );
            }

            if (index < props.Length - 1)
            {
                sourceWriter.WriteLine(",");
            }
            else
            {
                sourceWriter.WriteLine();
            }
        }

        sourceWriter.Indent--;
        sourceWriter.Write("}");

        sourceWriter.WriteLine(";");
    }
}
