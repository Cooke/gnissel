using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private void GenerateReader(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        WritePartialReadMappersClassStart(mappersClass, sourceWriter);
        sourceWriter.WriteLine();

        if (IsCustomMapped(type))
        {
            GenerateReaderProperty(sourceWriter, type, true);
            WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
            return;
        }

        if (GetMapTechnique(type) == MappingTechnique.AsIs)
        {
            GenerateReaderProperty(sourceWriter, type);
            GenerateReaderMetadata(sourceWriter, type, mappersClass);
            GenerateReadMethod(type, mappersClass, sourceWriter);
            WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
            return;
        }

        GenerateReaderProperty(sourceWriter, type);
        GenerateReaderMetadata(sourceWriter, type, mappersClass);

        if (type.IsValueType)
        {
            GenerateNullableReaderProperty(sourceWriter, type);
        }

        GenerateReadMethod(type, mappersClass, sourceWriter);

        if (type.IsValueType)
        {
            WriteNullableReadMethod(sourceWriter, type, mappersClass);
        }

        WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
    }

    private static bool IsCustomMapped(ITypeSymbol type) =>
        GetMapTechnique(type) == MappingTechnique.Custom;

    private static void GenerateReaderMetadata(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type,
        MappersClass mappersClass
    )
    {
        sourceWriter.Write("private ImmutableArray<ReadDescriptor> ");
        sourceWriter.Write(GetCreateReaderDescriptorsName(type));
        sourceWriter.WriteLine("() => [");
        sourceWriter.Indent++;
        if (
            IsBuildIn(type)
            || type.TypeKind == TypeKind.Enum
            || IsCustomMapped(type)
            || GetMapTechnique(type) == MappingTechnique.AsIs
        )
        {
            sourceWriter.WriteLine("new NextOrdinalReadDescriptor()");
        }
        else if (type.IsTupleType)
        {
            var ctorParameters = GetCtorParameters(type);
            for (var i = 0; i < ctorParameters.Length; i++)
            {
                sourceWriter.Write("..");
                sourceWriter.Write(GetReaderPropertyName(ctorParameters[i].Parameter.Type));
                sourceWriter.Write(".ReadDescriptors");
                if (i < ctorParameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
        }
        else
        {
            var ctorParameters = GetCtorParameters(type);
            var initializeProperties = GetInitializeProperties(type, ctorParameters);

            // Support "next reading" any type (not only build-in) which can be read by one read
            if (
                initializeProperties.Length + ctorParameters.Length == 1
                && IsBuildIn(
                    initializeProperties.FirstOrDefault()?.Type
                        ?? ctorParameters.First().Parameter.Type
                )
            )
            {
                sourceWriter.WriteLine("new NextOrdinalReadDescriptor()");
            }
            else
            {
                var newArgs = ctorParameters
                    .Select(x => new
                    {
                        Name = GetColumnName(mappersClass, x.Parameter, x.Property),
                        x.Parameter.Type,
                        Member = x.Parameter.Name,
                    })
                    .Concat(
                        initializeProperties.Select(x => new
                        {
                            Name = GetColumnName(mappersClass, x),
                            x.Type,
                            Member = x.Name,
                        })
                    )
                    .ToArray();

                for (int i = 0; i < newArgs.Length; i++)
                {
                    WriteSubReaderDescriptor(newArgs[i].Type, newArgs[i].Name, newArgs[i].Member);
                    if (i < newArgs.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }
            }
        }

        sourceWriter.Indent--;
        sourceWriter.WriteLine("];");
        sourceWriter.WriteLine();

        void WriteSubReaderDescriptor(ITypeSymbol typeSymbol, string? name, string member)
        {
            sourceWriter.Write("..");
            sourceWriter.Write(GetReaderPropertyName(typeSymbol));
            sourceWriter.Write(".ReadDescriptors.Select(d => d.WithParent(NameProvider, ");
            if (name != null)
            {
                sourceWriter.WriteStringOrNull(name);
                sourceWriter.Write(", ");
            }
            sourceWriter.WriteStringOrNull(member);
            sourceWriter.Write("))");
        }
    }

    private static void GenerateReaderProperty(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type,
        bool isRequired = false
    )
    {
        sourceWriter.Write("public");
        if (isRequired)
        {
            sourceWriter.Write(" required");
        }

        sourceWriter.Write(" ObjectReader<");
        sourceWriter.Write(type.ToDisplayString());
        if (
            type is
            {
                IsReferenceType: true,
                NullableAnnotation: NullableAnnotation.NotAnnotated or NullableAnnotation.None
            }
        )
        {
            sourceWriter.Write("?");
        }

        sourceWriter.Write("> ");
        sourceWriter.Write(GetReaderPropertyName(type));
        sourceWriter.WriteLine(" { get; init; }");
        sourceWriter.WriteLine();
    }

    private static void GenerateNullableReaderProperty(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write(AccessibilityToString(type.DeclaredAccessibility));
        sourceWriter.Write(" ObjectReader<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write("> ");
        sourceWriter.Write(GetNullableReaderPropertyName(type));
        sourceWriter.WriteLine(" { get; init; }");
        sourceWriter.WriteLine();
    }

    private static void WriteNullableReadMethod(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type,
        MappersClass mappersClass
    )
    {
        sourceWriter.Write("private ");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("? ");
        sourceWriter.Write(GetNullableReadMethodName(type));
        sourceWriter.WriteLine("(DbDataReader reader, OrdinalReader ordinalReader) {");
        sourceWriter.Indent++;

        GenerateReadMethodBody(type, mappersClass, sourceWriter);

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static string GetReadMethodName(ITypeSymbol type) =>
        $"Read{GetTypeIdentifierName(type)}";

    private static string GetNullableReadMethodName(ITypeSymbol type) =>
        $"Read{GetTypeIdentifierName(type)}Nullable";

    private static IPropertySymbol[] GetInitializeProperties(
        ITypeSymbol type,
        MappedParameter[]? ctorParameters
    ) =>
        type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                x.SetMethod is { DeclaredAccessibility: Accessibility.Public }
                && ctorParameters
                    ?.Select(p => p.Parameter.Name)
                    .Contains(x.Name, StringComparer.InvariantCultureIgnoreCase) != true
            )
            .ToArray();

    private record MappedProperty(IPropertySymbol Property, IParameterSymbol? Parameter)
    {
        public IPropertySymbol Property { get; } = Property;

        public IParameterSymbol? Parameter { get; } = Parameter;
    }

    private record MappedParameter(IParameterSymbol Parameter, IPropertySymbol? Property)
    {
        public IParameterSymbol Parameter { get; } = Parameter;

        public IPropertySymbol? Property { get; } = Property;
    }

    private static MappedParameter[]? GetCtorParametersOrNull(ITypeSymbol type) =>
        type.GetMembers(".ctor")
            .Where(x => x.DeclaredAccessibility != Accessibility.Private)
            .Cast<IMethodSymbol>()
            .OrderByDescending(x => x.Parameters.Length)
            .FirstOrDefault()
            ?.Parameters.Select(p => new MappedParameter(
                p,
                type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(x =>
                        x.Name.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase)
                    )
            ))
            .ToArray();

    private static MappedParameter[] GetCtorParameters(ITypeSymbol type) =>
        GetCtorParametersOrNull(type) ?? throw new InvalidOperationException();

    private static void WritePartialReadMappersClassStart(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        WritePartialMappersClassStart(mappersClass, sourceWriter);
        sourceWriter.WriteLine("public partial class DbReaders {");
        sourceWriter.Indent++;
    }

    private static void WritePartialReadMappersClassEnd(
        MappersClass mapperClass,
        IndentedTextWriter sourceWriter
    )
    {
        WritePartialMappersClassEnd(mapperClass, sourceWriter);
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void WritePartialMappersClassEnd(
        MappersClass mapperClass,
        IndentedTextWriter sourceWriter
    )
    {
        var type = mapperClass.Symbol;
        while (type != null)
        {
            sourceWriter.Indent--;
            sourceWriter.WriteLine("}");
            type = type.ContainingType;
        }
    }

    private static void WritePartialMappersClassStart(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter,
        bool withPrimaryConstructor = false
    )
    {
        sourceWriter.WriteLine("using System.Data.Common;");
        sourceWriter.WriteLine("using System.Collections.Immutable;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services.Implementations;");
        sourceWriter.WriteLine();
        WriteNamespace(mappersClass.Symbol.ContainingNamespace, sourceWriter);
        WritePartialClassStart(sourceWriter, mappersClass.Symbol);
        if (withPrimaryConstructor)
        {
            sourceWriter.WriteLine("(IDbNameProvider nameProvider)");
        }
        sourceWriter.Write(" : IMapperProvider ");
        sourceWriter.WriteLine(" {");
        sourceWriter.Indent++;
    }

    private static void WritePartialClassStart(
        IndentedTextWriter sourceWriter,
        INamedTypeSymbol cls
    )
    {
        if (cls.ContainingType != null)
        {
            WritePartialClassStart(sourceWriter, cls.ContainingType);
            sourceWriter.WriteLine(" {");
            sourceWriter.Indent++;
        }

        sourceWriter.Write(AccessibilityToString(cls.DeclaredAccessibility));
        sourceWriter.Write(" partial class ");
        sourceWriter.Write(cls.Name);
    }

    private static void WriteNamespace(INamespaceSymbol? ns, IndentedTextWriter sourceWriter)
    {
        if (ns == null || ns.Name == string.Empty)
        {
            return;
        }

        sourceWriter.Write("namespace ");
        WriteNamespaceParts(ns);
        sourceWriter.WriteLine(";");
        sourceWriter.WriteLine();

        void WriteNamespaceParts(INamespaceSymbol nsPart)
        {
            if (
                nsPart.ContainingNamespace != null
                && nsPart.ContainingNamespace.Name != string.Empty
            )
            {
                WriteNamespaceParts(nsPart.ContainingNamespace);
                sourceWriter.Write(".");
            }

            sourceWriter.Write(nsPart.Name);
        }
    }

    private static string AccessibilityToString(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "protected internal",
            _ => throw new ArgumentOutOfRangeException(),
        };

    private static string GetNullableReaderPropertyName(ITypeSymbol type) =>
        $"{GetTypeIdentifierName(type)}NullableReader";

    private static string GetReaderPropertyName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { Name: "Nullable" } nullableType)
        {
            return $"{GetTypeIdentifierName(nullableType.TypeArguments[0])}NullableReader";
        }

        return $"{GetTypeIdentifierName(type)}Reader";
    }

    private static string GetReaderPropertyName(string typeIdentifierName) =>
        $"{typeIdentifierName}Reader";
}
