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

        GenerateWriterDescriptors(mappersClass, sourceWriter, type);

        GenerateWriteMethod(type, mappersClass, sourceWriter);

        if (type.IsValueType)
        {
            WriteNullableWriteMethod(sourceWriter, type, mappersClass);
        }

        GenerateWriteMappersClassEnd(mappersClass, sourceWriter);
    }

    private static void GenerateWriterDescriptors(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("private ImmutableArray<WriteDescriptor> ");
        sourceWriter.Write(GetCreateWriterDescriptorsName(type));
        sourceWriter.WriteLine("() => [");
        sourceWriter.Indent++;
        if (
            IsBuildIn(type)
            || type.TypeKind == TypeKind.Enum
            || IsCustomMapped(type)
            || GetMapTechnique(type) == MappingTechnique.AsIs
        )
        {
            sourceWriter.WriteLine("new UnspecifiedColumnWriteDescriptor()");
        }
        else if (type.IsTupleType)
        {
            var ctorParameters = GetCtorParameters(type);
            for (var i = 0; i < ctorParameters.Length; i++)
            {
                sourceWriter.Write("..");
                sourceWriter.Write(GetWriterPropertyName(ctorParameters[i].Parameter.Type));
                sourceWriter.Write(".WriteDescriptors");
                if (i < ctorParameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
        }
        else
        {
            var props = GetWriteProperties(type);
            var ctorParameters = GetCtorParametersOrNull(type);

            if (props.Length == 1 && IsBuildIn(props.First().Type))
            {
                sourceWriter.WriteLine("new UnspecifiedColumnWriteDescriptor()");
            }
            else
            {
                for (int i = 0; i < props.Length; i++)
                {
                    var property = props[i];
                    var parameter = ctorParameters?.FirstOrDefault(x =>
                        x.Parameter.Name.Equals(
                            property.Name,
                            StringComparison.InvariantCultureIgnoreCase
                        )
                    );

                    WriteSubWriterDescriptor(property.Type, property, parameter?.Parameter);
                    if (i < props.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }
            }
        }

        sourceWriter.Indent--;
        sourceWriter.WriteLine("];");
        sourceWriter.WriteLine();

        void WriteSubWriterDescriptor(
            ITypeSymbol typeSymbol,
            IPropertySymbol property,
            IParameterSymbol? parameter
        )
        {
            sourceWriter.Write("..");
            sourceWriter.Write(GetWriterPropertyName(typeSymbol));
            sourceWriter.Write(".WriteDescriptors.Select(d => d.WithParent(NameProvider, ");
            var columnName = GetColumnName(mappersClass, property, parameter);
            if (columnName != null)
            {
                sourceWriter.WriteStringOrNull(columnName);
                sourceWriter.Write(", ");
            }
            sourceWriter.WriteStringOrNull(property.Name);
            sourceWriter.Write("))");
        }
    }

    private static IPropertySymbol[] GetWriteProperties(ITypeSymbol type)
    {
        var ctorParameters = GetCtorParametersOrNull(type);
        var props = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                x.DeclaredAccessibility == Accessibility.Public
                && !x.IsStatic
                && (
                    x
                        is {
                            IsReadOnly: false,
                            SetMethod.DeclaredAccessibility: Accessibility.Public
                        }
                    || ctorParameters?.Any(ctorParam =>
                        SymbolEqualityComparer.Default.Equals(ctorParam.Property, x)
                    ) == true
                )
            )
            .ToArray();
        return props;
    }

    private static string GetCreateWriterDescriptorsName(ITypeSymbol type) =>
        $"Create{GetTypeIdentifierName(AdjustNulls(type))}Descriptors";

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
