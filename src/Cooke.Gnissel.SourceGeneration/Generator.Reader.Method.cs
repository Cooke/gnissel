using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private void GenerateReadMethod(
        ITypeSymbol type,
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        sourceWriter.Write("private ");
        sourceWriter.Write(type.ToNullableDisplayString());
        sourceWriter.Write(" ");
        sourceWriter.Write(GetReadMethodName(type));
        sourceWriter.WriteLine("(DbDataReader reader, OrdinalReader ordinalReader) {");
        sourceWriter.Indent++;

        if (type.IsValueType)
        {
            sourceWriter.Write("return ");
            sourceWriter.Write(GetNullableReaderPropertyName(type));
            sourceWriter.Write(
                ".Read(reader, ordinalReader) ?? throw new InvalidOperationException(\"Expected non-null value\");"
            );
            sourceWriter.WriteLine(";");
        }
        else
        {
            GenerateReadMethodBody(type, mappersClass, sourceWriter);
        }

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();
    }

    private static void GenerateReadMethodBody(
        ITypeSymbol type,
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        if (IsBuildIn(type) || GetMapTechnique(type) == MappingTechnique.AsIs)
        {
            sourceWriter.Write("return ");
            GenerateNativeReadCall(type, sourceWriter);
            sourceWriter.WriteLine(";");
        }
        else if (
            type is INamedTypeSymbol { EnumUnderlyingType: not null and var underlyingEnumType }
        )
        {
            switch (mappersClass.EnumMappingTechnique)
            {
                case MappingTechnique.AsIs:
                    sourceWriter.Write("return reader.GetNullableValue<");
                    sourceWriter.Write(type.ToDisplayString());
                    sourceWriter.WriteLine(">(ordinalReader.Read());");
                    break;

                case MappingTechnique.AsString:
                    sourceWriter.WriteLine(
                        "var str = reader.GetValueOrNull<string>(ordinalReader.Read());"
                    );
                    sourceWriter.Write("return str is null ? null : ");
                    sourceWriter.Write("Enum.Parse<");
                    sourceWriter.Write(type.ToDisplayString());
                    sourceWriter.WriteLine(">(str);");
                    break;

                case MappingTechnique.AsInteger:
                    sourceWriter.Write("var val = reader.GetNullableValue<");
                    sourceWriter.Write(underlyingEnumType.ToDisplayString());
                    sourceWriter.WriteLine(">(ordinalReader.Read());");
                    sourceWriter.Write("return val is null ? null : ");
                    sourceWriter.Write("(");
                    sourceWriter.Write(type.ToDisplayString());
                    sourceWriter.WriteLine(")val;");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            var ctor = GetCtor(type);
            var ctorParameters = ctor.Parameters;
            var initializeProperties = GetInitializeProperties(type, ctor);

            foreach (var parameter in ctorParameters)
            {
                sourceWriter.Write("var ");
                sourceWriter.Write(parameter.Name);
                sourceWriter.Write(" = ");
                GenerateReadCall(parameter.Type, sourceWriter);
                sourceWriter.WriteLine(";");
            }

            foreach (var property in initializeProperties)
            {
                sourceWriter.Write("var ");
                sourceWriter.Write(property.Name);
                sourceWriter.Write(" = ");
                GenerateReadCall(property.Type, sourceWriter);
                sourceWriter.WriteLine(";");
            }

            sourceWriter.WriteLine();

            if (ctorParameters.Length > 0)
            {
                sourceWriter.Write("if (");
                var paramsAndProps = ctorParameters
                    .Select(x => x.Name)
                    .Concat(initializeProperties.Select(x => x.Name))
                    .ToArray();
                for (var i = 0; i < paramsAndProps.Length; i++)
                {
                    var parameter = paramsAndProps[i];
                    sourceWriter.Write(parameter);
                    sourceWriter.Write(" is null");

                    if (i < paramsAndProps.Length - 1)
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
            }

            sourceWriter.Write("return ");
            if (!type.IsTupleType)
            {
                sourceWriter.Write("new ");
                sourceWriter.Write(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            sourceWriter.WriteLine("(");
            sourceWriter.Indent++;
            for (var i = 0; i < ctorParameters.Length; i++)
            {
                var parameter = ctorParameters[i];
                sourceWriter.Write(parameter.Name);

                if (
                    parameter.Type is
                    { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated }
                )
                {
                    sourceWriter.Write(
                        " ?? throw new InvalidOperationException(\"Expected non-null value\")"
                    );
                }
                else if (parameter.Type.IsValueType && !IsNullableValueType(parameter.Type))
                {
                    sourceWriter.Write(
                        " ?? throw new InvalidOperationException(\"Expected non-null value\")"
                    );
                }

                if (i < ctorParameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }

            sourceWriter.Indent--;
            sourceWriter.Write(")");

            if (initializeProperties.Any())
            {
                sourceWriter.WriteLine(" {");
                sourceWriter.Indent++;
                for (var index = 0; index < initializeProperties.Length; index++)
                {
                    var property = initializeProperties[index];
                    sourceWriter.Write(property.Name);
                    sourceWriter.Write(" = ");
                    sourceWriter.Write(property.Name);

                    if (index < initializeProperties.Length - 1)
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
            }

            sourceWriter.WriteLine(";");
        }
    }

    private static void GenerateReadCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (type is INamedTypeSymbol { Name: "Nullable" } nullableType)
        {
            sourceWriter.Write(GetNullableReaderPropertyName(nullableType.TypeArguments[0]));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
        else if (type.IsValueType)
        {
            sourceWriter.Write(GetNullableReaderPropertyName(type));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
        else
        {
            sourceWriter.Write(GetReaderPropertyName(type));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
    }

    private static void GenerateNativeReadCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (IsNullableValueType(type) || type.IsValueType)
        {
            sourceWriter.Write("reader.GetNullableValue<");
        }
        else
        {
            sourceWriter.Write("reader.GetValueOrNull<");
        }
        sourceWriter.Write(
            type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()
        );
        sourceWriter.Write(">(ordinalReader.Read()");
        sourceWriter.Write(")");
    }
}
