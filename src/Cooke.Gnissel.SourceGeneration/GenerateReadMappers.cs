using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
    private static void WriteNotNullableReadMethod(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("private ");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write(" ");
        sourceWriter.Write(GetReadNotNullableMethodName(type));
        sourceWriter.WriteLine("(DbDataReader reader, OrdinalReader ordinalReader)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;

        sourceWriter.Write(GetReaderPropertyName(type));
        sourceWriter.WriteLine(".Read(reader, ordinalReader).Value;");

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();
    }

    private static void WriteReaderMetadata(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("private ImmutableArray<ReadDescriptor> ");
        sourceWriter.Write(GetCreateReaderDescriptorsName(type));
        sourceWriter.WriteLine(" => [");
        sourceWriter.Indent++;
        if (IsBuildIn(type) || type.TypeKind == TypeKind.Enum)
        {
            sourceWriter.WriteLine("new NextOrdinalReadDescriptor()");
        }
        else if (type.IsTupleType)
        {
            var ctor = GetCtor(type);
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                WriteSubReaderDescriptor(ctor.Parameters[i].Type, ctor.Parameters[i].Name);
                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
        }
        else
        {
            var ctor = GetCtor(type);
            var initializeProperties = GetInitializeProperties(type, ctor);

            // Support "next reading" any type (not only build-in) which can be read by one read
            if (
                initializeProperties.Length + ctor.Parameters.Length == 1
                && IsBuildIn(
                    initializeProperties.FirstOrDefault()?.Type ?? ctor.Parameters.First().Type
                )
            )
            {
                sourceWriter.WriteLine("new NextOrdinalObjectReadeDescriptor()");
            }
            else
            {
                var newArgs = ctor
                    .Parameters.Select(x => new { x.Name, x.Type })
                    .Concat(initializeProperties.Select(x => new { x.Name, x.Type }))
                    .ToArray();

                for (int i = 0; i < newArgs.Length; i++)
                {
                    WriteSubReaderDescriptor(newArgs[i].Type, newArgs[i].Name);;
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

        void WriteSubReaderDescriptor(ITypeSymbol typeSymbol, string name)
        {
            sourceWriter.Write("..");
            sourceWriter.Write(GetReaderPropertyName(typeSymbol));
            sourceWriter.Write(".ReadDescriptors.Select(d => d.WithParent(\"");
            sourceWriter.Write(name);
            sourceWriter.WriteLine("\")),");
        }
    }

    private static void WriteObjectReaderProperty(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("public ObjectReader<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write("> ");
        sourceWriter.Write(GetObjectReaderPropertyName(type));
        sourceWriter.WriteLine(";");
    }

    private static void WriteNotNullableObjectReaderProperty(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("public ObjectReader<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("> ");
        sourceWriter.Write(GetNotNullableObjectReaderPropertyName(type));
        sourceWriter.Write(";");
    }

    private static void WriteCreateReadMethodStart(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("private ");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write(" ");
        sourceWriter.Write(GetReadMethodName(type));
        sourceWriter.WriteLine("(DbDataReader reader, OrdinalReader ordinalReader) {");
        sourceWriter.Indent++;
    }

    private static string GetReadMethodName(ITypeSymbol type) =>
        $"Read{GetTypeIdentifierName(type)}";

    private static string GetReadNotNullableMethodName(ITypeSymbol type) =>
        $"ReadNotNull{GetTypeIdentifierName(type)}";

    private static void GenerateGetValue(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (type.IsValueType)
        {
            sourceWriter.Write("return reader.GetNullableValue<");
            sourceWriter.Write(type.ToDisplayString());
            sourceWriter.WriteLine(">(ordinalReader.Read());");
            return;
        }

        sourceWriter.Write("return reader.GetValueOrNull<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.WriteLine(">(ordinalReader.Read());");
    }

    private static IPropertySymbol[] GetInitializeProperties(
        ITypeSymbol type,
        IMethodSymbol ctor
    ) =>
        type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                x.SetMethod is { DeclaredAccessibility: Accessibility.Public }
                && !ctor
                    .Parameters.Select(p => p.Name)
                    .Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)
            )
            .ToArray();

    private static IMethodSymbol? GetCtorOrNull(ITypeSymbol type) =>
        type.GetMembers(".ctor")
            .Cast<IMethodSymbol>()
            .OrderByDescending(x => x.Parameters.Length)
            .FirstOrDefault();

    private static IMethodSymbol GetCtor(ITypeSymbol type) =>
        GetCtorOrNull(type) ?? throw new InvalidOperationException();

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
        IndentedTextWriter sourceWriter
    )
    {
        sourceWriter.WriteLine("using System.Data.Common;");
        sourceWriter.WriteLine("using System.Collections.Immutable;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services;");
        sourceWriter.WriteLine("using Cooke.Gnissel.SourceGeneration;");
        sourceWriter.WriteLine();
        WriteNamespace(mappersClass.Symbol.ContainingNamespace, sourceWriter);
        WritePartialClassStart(sourceWriter, mappersClass.Symbol);
    }

    private static void WritePartialClassStart(
        IndentedTextWriter sourceWriter,
        INamedTypeSymbol? cls
    )
    {
        if (cls == null)
        {
            return;
        }

        WritePartialClassStart(sourceWriter, cls.ContainingType);
        sourceWriter.Write(AccessibilityToString(cls.DeclaredAccessibility));
        sourceWriter.Write(" partial class ");
        sourceWriter.Write(cls.Name);
        sourceWriter.WriteLine(" {");
        sourceWriter.Indent++;
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

    private static string GetNotNullableObjectReaderPropertyName(ITypeSymbol type) =>
        "NotNullable" + GetObjectReaderPropertyName(GetTypeIdentifierName(type));

    private static string GetObjectReaderPropertyName(ITypeSymbol type) =>
        GetObjectReaderPropertyName(GetTypeIdentifierName(type));

    private static string GetObjectReaderPropertyName(string typeIdentifierName) =>
        $"{typeIdentifierName}Reader";
}
