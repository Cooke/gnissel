using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public class ReaderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var typesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node
                        is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name: GenericNameSyntax
                                {
                                    Identifier.ValueText: "Query",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            }
                        },
                (context, ct) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                    var dbContextTypeInfo = context.SemanticModel.GetTypeInfo(
                        memberAccess.Expression,
                        ct
                    );

                    if (!IsDbContext(dbContextTypeInfo))
                    {
                        return null;
                    }

                    var genericName = (GenericNameSyntax)memberAccess.Name;
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    var typeInfo = context.SemanticModel.GetTypeInfo(typeArg);
                    return typeInfo.Type;
                }
            )
            .Where(type => type != null)
            .Select((input, _) => input!)
            .Select(
                (type, _) =>
                {
                    if (type.IsReferenceType)
                    {
                        return type.WithNullableAnnotation(NullableAnnotation.Annotated);
                    }

                    if (type is INamedTypeSymbol { IsTupleType: true } tupleType)
                    {
                        return tupleType.ConstructedFrom.Construct(
                            tupleType
                                .TypeArguments.Select(x =>
                                    x.IsReferenceType
                                        ? x.WithNullableAnnotation(NullableAnnotation.Annotated)
                                        : x
                                )
                                .ToArray()
                        );
                    }

                    return type;
                }
            )
            .SelectMany(FindAllUsedTypes)
            .Collect()
            .SelectMany(
                (types, _) =>
                    types.Distinct(SymbolEqualityComparer.IncludeNullability).Cast<ITypeSymbol>()
            );

        initContext.RegisterImplementationSourceOutput(
            typesPipeline,
            (context, type) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter);
                WriteObjectReaderField(sourceWriter, type);
                WriteReaderMetadata(sourceWriter, type);
                WriteReaderMethodStart(sourceWriter, type);
                WriteReaderBody(type, sourceWriter);
                WriteReaderMethodEnd(sourceWriter);
                WritePartialReaderClassEnd(sourceWriter);

                context.AddSource(
                    $"GeneratedObjectReaderProvider.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        var typeIdentifierNames = typesPipeline.Select((x, _) => GetTypeIdentifierName(x));

        initContext.RegisterImplementationSourceOutput(
            typeIdentifierNames.Collect(),
            (context, names) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter);

                sourceWriter.WriteLine(
                    "private readonly ImmutableDictionary<Type, object> _objectReaders;"
                );
                sourceWriter.WriteLine();
                sourceWriter.WriteLine("public GeneratedObjectReaderProvider(IDbAdapter adapter)");
                sourceWriter.WriteLine("{");
                sourceWriter.Indent++;
                sourceWriter.WriteLine(
                    "var readers = ImmutableDictionary.CreateBuilder<Type, object>();"
                );
                sourceWriter.WriteLine();

                foreach (var name in names)
                {
                    sourceWriter.Write(GetObjectReaderFieldName(name));
                    sourceWriter.Write(" = ObjectReaderFactory.Create(adapter, Read");
                    sourceWriter.Write(name);
                    sourceWriter.Write(", Read");
                    sourceWriter.Write(name);
                    sourceWriter.WriteLine("Metadata);");
                    sourceWriter.Write("readers.Add(");
                    sourceWriter.Write(GetObjectReaderFieldName(name));
                    sourceWriter.Write(".ObjectType, ");
                    sourceWriter.Write(GetObjectReaderFieldName(name));
                    sourceWriter.WriteLine(");");
                    sourceWriter.WriteLine();
                }

                sourceWriter.WriteLine("_objectReaders = readers.ToImmutable();");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine(
                    "public ObjectReader<TOut> Get<TOut>(DbOptions dbOptions) =>"
                );
                sourceWriter.Indent++;
                sourceWriter.WriteLine("_objectReaders.TryGetValue(typeof(TOut), out var reader)");
                sourceWriter.WriteLine("? (ObjectReader<TOut>)reader");
                sourceWriter.WriteLine(
                    ": throw new InvalidOperationException(\"No reader found for type \" + typeof(TOut).Name);"
                );
                sourceWriter.Indent--;
                sourceWriter.WriteLine();

                WritePartialReaderClassEnd(sourceWriter);

                context.AddSource("GeneratedObjectReaderProvider.cs", stringWriter.ToString());
            }
        );
    }

    private IEnumerable<ITypeSymbol> FindAllUsedTypes(ITypeSymbol type, CancellationToken ct)
    {
        yield return type;

        if (IsPrimitive(type))
        {
            yield break;
        }

        var ctor = GetCtorOrNull(type);
        if (ctor == null)
        {
            yield break;
        }

        foreach (var t in ctor.Parameters)
        {
            if (!IsPrimitive(t.Type))
            {
                foreach (var innerType in FindAllUsedTypes(t.Type, ct))
                {
                    yield return innerType;
                }
            }
        }
    }

    private void WriteReaderMetadata(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        if (IsPrimitive(type))
        {
            sourceWriter.Write("private static readonly NextOrdinalReaderMetadata ");
            sourceWriter.Write(GetReaderMetadataName(type));
            sourceWriter.WriteLine(" = new ();");
        }
        else if (type.IsTupleType)
        {
            sourceWriter.Write("private static MultiReaderMetadata ");
            sourceWriter.Write(GetReaderMetadataName(type));
            sourceWriter.WriteLine(" => new ([");
            sourceWriter.Indent++;
            var ctor = GetCtor(type);
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                sourceWriter.Write(GetReaderMetadataName(ctor.Parameters[i].Type));

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

            sourceWriter.Write("private static MultiReaderMetadata ");
            sourceWriter.Write("Read");
            sourceWriter.Write(GetTypeIdentifierName(type));
            sourceWriter.Write("Metadata");
            sourceWriter.WriteLine(" => new ([");
            sourceWriter.Indent++;

            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                if (IsPrimitive(parameter.Type))
                {
                    sourceWriter.Write("new NameReaderMetadata(\"");
                    sourceWriter.Write(parameter.Name);
                    sourceWriter.Write("\")");
                }
                else
                {
                    sourceWriter.Write("new NestedReaderMetadata(\"");
                    sourceWriter.Write(parameter.Name);
                    sourceWriter.Write("\", ");
                    sourceWriter.Write(GetReaderMetadataName(parameter.Type));
                    sourceWriter.Write(")");
                }

                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }

            sourceWriter.Indent--;
            sourceWriter.WriteLine("]);");
        }

        sourceWriter.WriteLine();
    }

    private static string GetReaderMetadataName(ITypeSymbol type)
    {
        return $"Read{GetTypeIdentifierName(type)}Metadata";
    }

    private static string GetTypeIdentifierName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { Name: "Nullable", TypeArguments.Length: 1 } namedTypeSymbol)
        {
            return "Nullable" + GetTypeIdentifierName(namedTypeSymbol.TypeArguments[0]);
        }

        var baseName = string.Join(
            "",
            type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat)
                .Select(x => x.Symbol?.Name)
                .Where(x => !string.IsNullOrEmpty(x))
        );

        if (type.ContainingType != null)
        {
            return GetTypeIdentifierName(type.ContainingType) + baseName;
        }

        return baseName;
    }

    private static void WriteObjectReaderField(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("private readonly ObjectReader<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("> ");
        sourceWriter.Write(GetObjectReaderFieldName(type));
        sourceWriter.WriteLine(";");
        sourceWriter.WriteLine();
    }

    private static string GetObjectReaderFieldName(ITypeSymbol type) =>
        GetObjectReaderFieldName(GetTypeIdentifierName(type));

    private static string GetObjectReaderFieldName(string typeDisplayName) =>
        $"_{char.ToLower(typeDisplayName[0])}{typeDisplayName.Substring(1)}Reader";

    private static void WriteReaderMethodStart(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("public ");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write(" Read");
        sourceWriter.Write(GetTypeIdentifierName(type));
        sourceWriter.WriteLine("(DbDataReader reader, OrdinalReader ordinalReader)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private void WriteReaderBody(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (IsPrimitive(type))
        {
            sourceWriter.Write("return ");
            WriteReadCall(type, sourceWriter);

            if (!IsNullable(type))
            {
                sourceWriter.Write(" ?? ");
                sourceWriter.Write(
                    "throw new InvalidOperationException(\"Expected non-null value\")"
                );
            }

            sourceWriter.WriteLine(";");
        }
        else
        {
            var ctor = GetCtor(type);

            for (var i = 0; i < ctor!.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                sourceWriter.Write("var ");
                sourceWriter.Write(parameter.Name);
                sourceWriter.Write(" = ");
                WriteReadCall(parameter.Type, sourceWriter);
                sourceWriter.WriteLine(";");
            }
            sourceWriter.WriteLine();

            if (IsNullable(type) && ctor.Parameters.Length > 0)
            {
                sourceWriter.Write("if (");
                for (var i = 0; i < ctor.Parameters.Length; i++)
                {
                    var parameter = ctor.Parameters[i];
                    sourceWriter.Write(parameter.Name);
                    sourceWriter.Write(" is null");

                    if (i < ctor.Parameters.Length - 1)
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
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                sourceWriter.Write(parameter.Name);

                if (parameter.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    sourceWriter.Write(
                        " ?? throw new InvalidOperationException(\"Expected non-null value\")"
                    );
                }

                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
            sourceWriter.Indent--;
            sourceWriter.WriteLine(");");
        }
    }

    private static IMethodSymbol? GetCtorOrNull(ITypeSymbol type) =>
        type.GetMembers(".ctor")
            .Cast<IMethodSymbol>()
            .OrderByDescending(x => x.Parameters.Length)
            .FirstOrDefault();

    private static IMethodSymbol GetCtor(ITypeSymbol type) =>
        GetCtorOrNull(type) ?? throw new InvalidOperationException();

    private void WriteReadCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (IsPrimitive(type))
        {
            WriteDbReaderNullableCall(type, sourceWriter);
        }
        else if (type.IsValueType)
        {
            sourceWriter.Write(or);
            sourceWriter.Write(GetObjectReaderFieldName(type));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
        else
        {
            sourceWriter.Write(GetObjectReaderFieldName(type));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
    }

    private void WriteDbReaderNullableCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        sourceWriter.Write("reader.Get");
        sourceWriter.Write(GetReaderGetSuffix(type));
        sourceWriter.Write("OrNull(ordinalReader.Read()");
        sourceWriter.Write(")");
    }

    private static void WriteReaderMethodEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void WritePartialReaderClassStart(IndentedTextWriter sourceWriter)
    {
        sourceWriter.WriteLine("namespace Gnissel.SourceGeneration;");
        sourceWriter.WriteLine();
        sourceWriter.WriteLine("using System.Data.Common;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.SourceGeneration;");
        sourceWriter.WriteLine("using System.Collections.Immutable;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services;");
        sourceWriter.WriteLine();
        sourceWriter.WriteLine("public partial class GeneratedObjectReaderProvider");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private static void WritePartialReaderClassEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.Flush();
    }

    private bool IsNullable(ITypeSymbol type) =>
        type is { Name: "Nullable" } || type.IsReferenceType;

    private bool IsPrimitive(ITypeSymbol readTypeType) =>
        readTypeType.Name switch
        {
            "Int32" or "String" or "DateTime" or "TimeSpan" => true,
            "Nullable" => true,
            _ => false,
        };

    private static bool IsDbContext(TypeInfo dbContextTypeInfo)
    {
        return dbContextTypeInfo.Type?.BaseType?.Name == "DbContext"
            || dbContextTypeInfo.Type?.Name == "DbContext";
    }

    private string GetReaderGetSuffix(ITypeSymbol type) =>
        type switch
        {
            { Name: "Nullable" } and INamedTypeSymbol namedTypeSymbol => namedTypeSymbol
                .TypeArguments[0]
                .Name,
            _ => type.Name,
        };
}
