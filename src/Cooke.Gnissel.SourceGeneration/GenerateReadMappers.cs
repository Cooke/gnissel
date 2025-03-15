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
        sourceWriter.Write("private static ObjectReaderFunc<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("> ");
        sourceWriter.Write(GetCreateNotNullableReadMethodName(type));
        sourceWriter.WriteLine("(ObjectReaderCreateContext context)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;

        sourceWriter.Write("var ");
        sourceWriter.Write(GetReaderVariableName(type));
        sourceWriter.Write(" = context.ReaderProvider.Get<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.WriteLine(">();");

        sourceWriter.Write("return (reader, ordinalReader) => ");
        sourceWriter.Write(GetReaderVariableName(type));
        sourceWriter.WriteLine(".Read(reader, ordinalReader).Value;");

        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.WriteLine();
    }

    private static void WriteReaderMetadata(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("private static readonly ObjectReaderMetadata ");
        sourceWriter.Write(GetReaderMetadataName(type));
        if (IsBuildIn(type) || type.TypeKind == TypeKind.Enum)
        {
            sourceWriter.WriteLine(" = new NextOrdinalObjectReaderMetadata();");
        }
        else if (type.IsTupleType)
        {
            sourceWriter.WriteLine(" = new MultiObjectReaderMetadata([");
            sourceWriter.Indent++;
            var ctor = GetCtor(type);
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                sourceWriter.Write("new NestedObjectReaderMetadata(typeof(");
                sourceWriter.Write(
                    ctor.Parameters[i].Type.ToDisplayString(NullableFlowState.NotNull)
                );
                sourceWriter.Write("))");
                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }

            sourceWriter.Indent--;
            sourceWriter.WriteLine("]);");
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
                sourceWriter.WriteLine(" = new NextOrdinalObjectReaderMetadata();");
            }
            else
            {
                sourceWriter.WriteLine(" = new MultiObjectReaderMetadata([");
                sourceWriter.Indent++;

                var newArgs = ctor
                    .Parameters.Select(x => new { x.Name, x.Type })
                    .Concat(initializeProperties.Select(x => new { x.Name, x.Type }))
                    .ToArray();

                for (int i = 0; i < newArgs.Length; i++)
                {
                    var arg = newArgs[i];
                    sourceWriter.Write("new NameObjectReaderMetadata(\"");
                    sourceWriter.Write(arg.Name);
                    sourceWriter.Write("\"");
                    if (!BuildInDirectlyMappedTypes.Contains(arg.Type.Name))
                    {
                        sourceWriter.Write(", new NestedObjectReaderMetadata(typeof(");
                        sourceWriter.Write(arg.Type.ToDisplayString(NullableFlowState.NotNull));
                        sourceWriter.Write("))");
                    }

                    sourceWriter.Write(")");

                    if (i < newArgs.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("]);");
            }
        }

        sourceWriter.WriteLine();
    }

    private static void WriteObjectReaderDescriptorField(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("public static readonly ObjectReaderDescriptor<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write("> ");
        sourceWriter.Write(GetObjectReaderDescriptorFieldName(type));
        sourceWriter.Write(" = new(");
        sourceWriter.Write(GetCreateReadMethodName(type));
        sourceWriter.Write(", ");
        sourceWriter.Write(GetReaderMetadataName(type));
        sourceWriter.WriteLine(");");
        sourceWriter.WriteLine();
    }

    private static void WriteNotNullableObjectReaderDescriptorField(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("public static readonly ObjectReaderDescriptor<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("> ");
        sourceWriter.Write(GetNotNullableObjectReaderDescriptorFieldName(type));
        sourceWriter.Write(" = new(");
        sourceWriter.Write(GetCreateNotNullableReadMethodName(type));
        sourceWriter.Write(", ");
        sourceWriter.Write(GetReaderMetadataName(type));
        sourceWriter.WriteLine(");");
        sourceWriter.WriteLine();
    }

    private static void WriteCreateReadMethodStart(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write("private static ObjectReaderFunc<");
        WriteTypeNameEnsureNullable(sourceWriter, type);
        sourceWriter.Write("> ");
        sourceWriter.Write(GetCreateReadMethodName(type));
        sourceWriter.WriteLine("(ObjectReaderCreateContext context)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;

        if (!IsBuildIn(type))
        {
            var ctor = GetCtor(type);
            var typeSymbols = ctor
                .Parameters.Select(x => x.Type)
                .Where(x => !BuildInDirectlyMappedTypes.Contains(x.Name))
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<ITypeSymbol>()
                .ToArray();
            foreach (var usedType in typeSymbols)
            {
                sourceWriter.Write("var ");
                sourceWriter.Write(GetReaderVariableName(usedType));
                sourceWriter.Write(" = context.ReaderProvider.Get<");
                WriteTypeNameEnsureNullable(sourceWriter, usedType);
                sourceWriter.WriteLine(">();");
            }

            if (typeSymbols.Any())
            {
                sourceWriter.WriteLine();
            }
        }

        sourceWriter.WriteLine("return (reader, ordinalReader) =>");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private static string GetCreateReadMethodName(ITypeSymbol type) =>
        $"Create{GetTypeIdentifierName(type)}ReadFunc";

    private static string GetCreateNotNullableReadMethodName(ITypeSymbol type) =>
        $"CreateNotNullable{GetTypeIdentifierName(type)}ReadFunc";

    private static void WriteGetValue(ITypeSymbol type, IndentedTextWriter sourceWriter)
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
        sourceWriter.WriteLine("public static partial class ReadMappers {");
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

    private static string GetNotNullableObjectReaderDescriptorFieldName(ITypeSymbol type) =>
        "NotNullable" + GetObjectReaderDescriptorFieldName(GetTypeIdentifierName(type));

    private static string GetObjectReaderDescriptorFieldName(ITypeSymbol type) =>
        GetObjectReaderDescriptorFieldName(GetTypeIdentifierName(type));

    private static string GetObjectReaderDescriptorFieldName(string typeIdentifierName) =>
        $"{typeIdentifierName}ReaderDescriptor";
}
