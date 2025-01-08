using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public class ReaderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var dbContextsPipline = initContext.SyntaxProvider.ForAttributeWithMetadataName(
            "Cooke.Gnissel.DbContextAttribute",
            (node, _) => node is ClassDeclarationSyntax,
            (context, _) => (INamedTypeSymbol)context.TargetSymbol
        );

        initContext.RegisterSourceOutput(
            dbContextsPipline,
            (context, dbContextType) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialDbContextClass(sourceWriter, dbContextType);
                sourceWriter.Flush();
                context.AddSource(
                    GetDbContextIdentifierName(dbContextType) + ".cs",
                    stringWriter.ToString()
                );
            }
        );

        var dbContextTypesPipline = initContext
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
                    var instanceType = context.SemanticModel.GetTypeInfo(
                        memberAccess.Expression,
                        ct
                    );

                    if (
                        instanceType
                            .Type?.GetAttributes()
                            .Any(x => x.AttributeClass?.Name == "DbContextAttribute") != true
                    )
                    {
                        return null;
                    }

                    var genericName = (GenericNameSyntax)memberAccess.Name;
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    var typeArgInfo = context.SemanticModel.GetTypeInfo(typeArg);
                    if (typeArgInfo.Type is null)
                    {
                        return null;
                    }

                    return new DbContextType(instanceType.Type, typeArgInfo.Type);
                }
            )
            .Where(type => type != null)
            .Select((input, _) => input!)
            // Currently indirect usage is not supporte (unbound type parameters)
            .Where(type => type.Type is not ITypeParameterSymbol)
            .Collect()
            .SelectMany(
                (invocations, _) =>
                    invocations
                        .GroupBy(x => x.DbContext, SymbolEqualityComparer.Default)
                        .SelectMany(contextTypes =>
                            contextTypes
                                .Select(x => x.Type)
                                .SelectMany(FindAllUsedTypes)
                                .Select(AdjustNulls)
                                .Distinct(SymbolEqualityComparer.Default)
                                .Cast<ITypeSymbol>()
                                .Select(type => new DbContextType(
                                    (ITypeSymbol)contextTypes.Key!,
                                    type
                                ))
                        )
            );

        initContext.RegisterImplementationSourceOutput(
            dbContextTypesPipline,
            (context, dbContextType) =>
            {
                var type = dbContextType.Type;
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter, dbContextType.DbContext);
                sourceWriter.WriteLine();
                WriteReaderMetadata(sourceWriter, type);
                WriteObjectReaderDescriptorField(sourceWriter, type);
                WriteCreateReadMethodStart(sourceWriter, type);
                WriteReaderBody(type, sourceWriter);
                WriteCreateReadMethodEnd(sourceWriter);
                WritePartialReaderClassEnd(sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetDbContextIdentifierName(dbContextType.DbContext)}.Readers.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        initContext.RegisterImplementationSourceOutput(
            dbContextTypesPipline
                .Select((x, _) => new { x.DbContext, TypeName = GetTypeIdentifierName(x.Type) })
                .Collect()
                .SelectMany(
                    (dbContextTypes, _) =>
                        dbContextTypes
                            .GroupBy(x => x.DbContext, SymbolEqualityComparer.Default)
                            .Select(group => new
                            {
                                DbContext = (ITypeSymbol)group.Key!,
                                TypeNames = group.Select(x => x.TypeName).ToArray(),
                            })
                ),
            (context, dbContextTypeNames) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter, dbContextTypeNames.DbContext);

                sourceWriter.WriteLine(
                    "public static IObjectReaderProvider CreateProvider(IDbAdapter adapter) =>"
                );
                sourceWriter.WriteLine(
                    "new ObjectReaderProviderBuilder(ObjectReaderDescriptors).Build(adapter);"
                );
                sourceWriter.WriteLine();

                sourceWriter.WriteLine(
                    "public static readonly ImmutableArray<IObjectReaderDescriptor> ObjectReaderDescriptors;"
                );
                sourceWriter.WriteLine();
                sourceWriter.WriteLine("static ObjectReaders() {");
                sourceWriter.Indent++;
                sourceWriter.WriteLine("ObjectReaderDescriptors = [");
                sourceWriter.Indent++;

                var names = dbContextTypeNames.TypeNames;
                for (var index = 0; index < names.Length; index++)
                {
                    var name = names[index];
                    sourceWriter.Write(GetObjectReaderDescriptorFieldName(name));

                    if (index < names.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }
                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");

                WritePartialReaderClassEnd(sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetDbContextIdentifierName(dbContextTypeNames.DbContext)}.Readers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }

    private static ITypeSymbol AdjustNulls(ITypeSymbol type, CancellationToken ct) =>
        AdjustNulls(type);

    private static ITypeSymbol AdjustNulls(ITypeSymbol type)
    {
        return type switch
        {
            INamedTypeSymbol { Name: "Nullable", TypeArguments.Length: 1 } nullable =>
                nullable.TypeArguments[0],

            INamedTypeSymbol { IsTupleType: true } tupleType => tupleType.ConstructedFrom.Construct(
                tupleType
                    .TupleElements.Select(x =>
                        x.Type.IsReferenceType ? AdjustNulls(x.Type) : x.Type
                    )
                    .ToArray()
            ),

            { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated } =>
                type.WithNullableAnnotation(NullableAnnotation.Annotated),
            _ => type,
        };
    }

    private IEnumerable<ITypeSymbol> FindAllUsedTypes(ITypeSymbol type)
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
                foreach (var innerType in FindAllUsedTypes(t.Type))
                {
                    yield return innerType;
                }
            }
        }
    }

    private void WriteReaderMetadata(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("private static readonly ObjectReaderMetadata ");
        sourceWriter.Write(GetReaderMetadataName(type));
        if (IsPrimitive(type))
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
            sourceWriter.WriteLine(" = new MultiObjectReaderMetadata([");
            sourceWriter.Indent++;
            var ctor = GetCtor(type);

            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                sourceWriter.Write("new NameObjectReaderMetadata(\"");
                sourceWriter.Write(parameter.Name);
                sourceWriter.Write("\"");
                if (!IsPrimitive(parameter.Type))
                {
                    sourceWriter.Write(", new NestedObjectReaderMetadata(typeof(");
                    sourceWriter.Write(parameter.Type.ToDisplayString(NullableFlowState.NotNull));
                    sourceWriter.Write("))");
                }

                sourceWriter.Write(")");

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
        return $"{GetTypeIdentifierName(AdjustNulls(type))}ReaderMetadata";
    }

    private static string GetTypeIdentifierName(ITypeSymbol type)
    {
        var baseName = GetBaseName(type);

        if (type.ContainingType != null)
        {
            return GetTypeIdentifierName(type.ContainingType) + baseName;
        }

        return baseName;

        static string GetBaseName(ITypeSymbol type) =>
            type switch
            {
                INamedTypeSymbol { Name: "Nullable" } nullableType => "Nullable"
                    + GetBaseName(nullableType.TypeArguments[0]),

                INamedTypeSymbol { IsTupleType: true } tupleType => string.Join(
                    "",
                    tupleType.TypeArguments.Select(GetBaseName)
                ),

                _ => string.Join(
                    "",
                    type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat)
                        .Select(x => x.Symbol?.Name)
                        .Where(x => !string.IsNullOrEmpty(x))
                ),
            };
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

    private static string GetObjectReaderDescriptorFieldName(ITypeSymbol type) =>
        GetObjectReaderDescriptorFieldName(GetTypeIdentifierName(type));

    private static string GetObjectReaderDescriptorFieldName(string typeIdentifierName) =>
        $"{typeIdentifierName}ReaderDescriptor";

    private static void WriteTypeNameEnsureNullable(
        IndentedTextWriter sourceWriter,
        ITypeSymbol type
    )
    {
        sourceWriter.Write(type.ToDisplayString());
        if (type.NullableAnnotation != NullableAnnotation.Annotated)
        {
            sourceWriter.Write("?");
        }
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

        if (!IsPrimitive(type))
        {
            var ctor = GetCtor(type);
            var typeSymbols = ctor
                .Parameters.Select(x => x.Type)
                .Where(x => !IsPrimitive(x))
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

    private static string GetReaderVariableName(ITypeSymbol usedType)
    {
        var typeIdentifierName = GetTypeIdentifierName(usedType);
        return char.ToLower(typeIdentifierName[0]) + typeIdentifierName.Substring(1) + "Reader";
    }

    private void WriteReaderBody(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (IsPrimitive(type))
        {
            sourceWriter.Write("return ");
            WriteReadCall(type, sourceWriter);
            sourceWriter.WriteLine(";");
        }
        else
        {
            var ctor = GetCtor(type);

            foreach (var parameter in ctor.Parameters)
            {
                sourceWriter.Write("var ");
                sourceWriter.Write(parameter.Name);
                sourceWriter.Write(" = ");
                WriteReadCall(parameter.Type, sourceWriter);
                sourceWriter.WriteLine(";");
            }
            sourceWriter.WriteLine();

            if (IsNullableValueTypeOrReferenceType(type) && ctor.Parameters.Length > 0)
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
        else
        {
            sourceWriter.Write(GetReaderVariableName(type));
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

    private static void WriteCreateReadMethodEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("};");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void WritePartialReaderClassStart(
        IndentedTextWriter sourceWriter,
        ITypeSymbol dbContextType
    )
    {
        if (!dbContextType.ContainingNamespace.IsGlobalNamespace)
        {
            sourceWriter.Write("namespace ");
            sourceWriter.Write(dbContextType.ContainingNamespace.ToDisplayString());
            sourceWriter.WriteLine(";");
        }

        sourceWriter.WriteLine("using System.Data.Common;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.SourceGeneration;");
        sourceWriter.WriteLine("using System.Collections.Immutable;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services;");
        sourceWriter.WriteLine();
        sourceWriter.Write("public partial class ");
        sourceWriter.Write(GetDbContextIdentifierName(dbContextType));
        sourceWriter.WriteLine(" {");
        sourceWriter.Indent++;
        sourceWriter.WriteLine("public static partial class ObjectReaders {");
        sourceWriter.Indent++;
    }

    private static string GetDbContextIdentifierName(ITypeSymbol dbContextType) =>
        dbContextType.Name;

    private static void WritePartialDbContextClass(
        IndentedTextWriter sourceWriter,
        ITypeSymbol dbContextType
    )
    {
        if (!dbContextType.ContainingNamespace.IsGlobalNamespace)
        {
            sourceWriter.Write("namespace ");
            sourceWriter.Write(dbContextType.ContainingNamespace.ToDisplayString());
            sourceWriter.WriteLine(";");
        }

        sourceWriter.WriteLine("using System.Data.Common;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services;");
        sourceWriter.WriteLine();
        sourceWriter.Write("public partial class ");
        sourceWriter.Write(dbContextType.Name);
        sourceWriter.WriteLine("(DbOptions options) : DbContext(options) {");
        sourceWriter.Indent++;
        sourceWriter.Write("public ");
        sourceWriter.Write(dbContextType.Name);
        sourceWriter.WriteLine(
            " (IDbAdapter adapter) : this(new DbOptions(adapter, ObjectReaders.CreateProvider(adapter))) { }"
        );
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void WritePartialReaderClassEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private bool IsNullableValueTypeOrReferenceType(ITypeSymbol type) =>
        IsNullableValueType(type) || type.IsReferenceType;

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type is { Name: "Nullable" };
    }

    private static bool IsPrimitive(ITypeSymbol readTypeType) =>
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

    private record DbContextType(ITypeSymbol DbContext, ITypeSymbol Type)
    {
        public ITypeSymbol DbContext { get; } = DbContext;

        public ITypeSymbol Type { get; } = Type;
    }
}
