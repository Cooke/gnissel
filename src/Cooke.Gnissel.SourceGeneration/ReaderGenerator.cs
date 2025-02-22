using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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

        var selectTypesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node
                        is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name.Identifier.ValueText: "Select"
                            },
                            ArgumentList.Arguments.Count: 1
                        },
                (context, ct) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var operation = context.SemanticModel.GetOperation(invocation, ct);
                    if (
                        operation
                        is not IInvocationOperation
                        {
                            TargetMethod: { TypeArguments: { Length: 1 } typeArguments }
                        }
                    )
                    {
                        return null;
                    }

                    var typeArg = typeArguments[0];
                    return typeArg;
                }
            )
            .Where(x => x != null)
            .Select((v, _) => v!);

        initContext.RegisterImplementationSourceOutput(
            selectTypesPipeline,
            (context, type) =>
            {
                context.AddSource($"Fun.1.cs", type.ToDisplayString());
            }
        );

        var dbContextTypesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node
                        is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name: GenericNameSyntax
                                {
                                    Identifier.ValueText: "Query"
                                        or "QuerySingle"
                                        or "QuerySingleOrDefault",
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
            // Currently indirect usage is not supported (unbound type parameters)
            .Where(type => type.Type is not ITypeParameterSymbol)
            .Collect()
            .SelectMany(
                (invocations, _) =>
                    invocations
                        .GroupBy(x => x.DbContext, SymbolEqualityComparer.Default)
                        .SelectMany(contextTypes =>
                            contextTypes
                                .Select(x => x.Type)
                                .SelectMany(FindAllReaderTypes)
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
            dbContextTypesPipeline,
            (context, dbContextType) =>
            {
                var type = dbContextType.Type;
                var dbContextOptions = GetDbContextOptions(dbContextType);

                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter, dbContextType.DbContext);
                sourceWriter.WriteLine();
                WriteReaderMetadata(sourceWriter, type);
                WriteObjectReaderDescriptorField(sourceWriter, type);

                if (type.IsValueType)
                {
                    WriteNotNullableObjectReaderDescriptorField(sourceWriter, type);
                    WriteNotNullableReadMethod(sourceWriter, type);
                }

                WriteCreateReadMethodStart(sourceWriter, type);
                WriteReaderBody(type, dbContextOptions, sourceWriter);
                WriteCreateReadMethodEnd(sourceWriter);
                WritePartialReaderClassEnd(sourceWriter, dbContextType.DbContext);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetDbContextIdentifierName(dbContextType.DbContext)}.Readers.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        initContext.RegisterImplementationSourceOutput(
            dbContextTypesPipeline
                .Select((x, _) => new { x.DbContext, x.Type })
                .Collect()
                .SelectMany(
                    (dbContextTypes, _) =>
                        dbContextTypes
                            .GroupBy(x => x.DbContext, SymbolEqualityComparer.Default)
                            .Select(group => new
                            {
                                DbContext = (ITypeSymbol)group.Key!,
                                Types = group.Select(x => x.Type).ToArray(),
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

                var types = dbContextTypeNames.Types;
                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write(GetObjectReaderDescriptorFieldName(type));
                    if (type.IsValueType)
                    {
                        sourceWriter.WriteLine(",");
                        sourceWriter.Write(GetNotNullableObjectReaderDescriptorFieldName(type));
                    }

                    if (index < types.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }
                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");

                WritePartialReaderClassEnd(sourceWriter, dbContextTypeNames.DbContext);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetDbContextIdentifierName(dbContextTypeNames.DbContext)}.Readers.cs",
                    stringWriter.ToString()
                );
            }
        );
    }

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

    private static DbContextOptions GetDbContextOptions(DbContextType dbContextType)
    {
        var dbContextArguments = dbContextType
            .DbContext.GetAttributes()
            .First(x => x.AttributeClass?.Name == "DbContextAttribute")
            .NamedArguments;

        EnumMappingTechnique? enumMappingTechnique = null;
        foreach (var dbContextArgument in dbContextArguments)
        {
            switch (dbContextArgument.Key)
            {
                case "EnumMappingTechnique":
                    enumMappingTechnique = (EnumMappingTechnique)dbContextArgument.Value.Value;
                    break;
            }
        }
        return new DbContextOptions(
            enumMappingTechnique.GetValueOrDefault(EnumMappingTechnique.Direct)
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

    private IEnumerable<ITypeSymbol> FindAllReaderTypes(ITypeSymbol type)
    {
        yield return type;

        if (IsBuildIn(type))
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
            if (!BuildInDirectlyMappedTypes.Contains(t.Type.Name))
            {
                foreach (var innerType in FindAllReaderTypes(t.Type))
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
            sourceWriter.WriteLine(" = new MultiObjectReaderMetadata([");
            sourceWriter.Indent++;
            var ctor = GetCtor(type);
            var initializeProperties = GetInitializeProperties(type, ctor);
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

    private static string GetNotNullableObjectReaderDescriptorFieldName(ITypeSymbol type) =>
        "NotNullable" + GetObjectReaderDescriptorFieldName(GetTypeIdentifierName(type));

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
        if (type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated)
        {
            sourceWriter.Write("?");
        }
        else if (type.IsValueType && !IsNullableValueType(type))
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

    private static string GetReaderVariableName(ITypeSymbol usedType)
    {
        var typeIdentifierName = GetTypeIdentifierName(usedType);
        return char.ToLower(typeIdentifierName[0]) + typeIdentifierName.Substring(1) + "Reader";
    }

    private void WriteReaderBody(
        ITypeSymbol type,
        DbContextOptions dbContextOptions,
        IndentedTextWriter sourceWriter
    )
    {
        if (IsBuildIn(type))
        {
            sourceWriter.Write("return reader.GetValueOrNull<");
            sourceWriter.Write(type.ToDisplayString());
            sourceWriter.WriteLine(">(ordinalReader.Read());");
        }
        else if (
            type is INamedTypeSymbol { EnumUnderlyingType: not null and var underlyingEnumType }
        )
        {
            switch (dbContextOptions.EnumMappingTechnique)
            {
                case EnumMappingTechnique.Direct:
                    sourceWriter.Write("return reader.GetValueOrNull<");
                    sourceWriter.Write(type.ToDisplayString());
                    sourceWriter.WriteLine(">(ordinalReader.Read());");
                    break;

                case EnumMappingTechnique.String:
                    sourceWriter.WriteLine(
                        "var str = reader.GetStringOrNull(ordinalReader.Read());"
                    );
                    sourceWriter.Write("return str is null ? null : ");
                    sourceWriter.Write("Enum.Parse<");
                    sourceWriter.Write(type.ToDisplayString());
                    sourceWriter.WriteLine(">(str);");
                    break;

                case EnumMappingTechnique.Value:
                    sourceWriter.Write("var val = reader.GetValueOrNull<");
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
                WriteReadCall(parameter.Type, sourceWriter);
                sourceWriter.WriteLine(";");
            }
            foreach (var property in initializeProperties)
            {
                sourceWriter.Write("var ");
                sourceWriter.Write(property.Name);
                sourceWriter.Write(" = ");
                WriteReadCall(property.Type, sourceWriter);
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
                    sourceWriter.Write(".Value");
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

    private static IPropertySymbol[] GetInitializeProperties(ITypeSymbol type, IMethodSymbol ctor)
    {
        var initializeProperties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                x.SetMethod is { DeclaredAccessibility: Accessibility.Public }
                && !ctor
                    .Parameters.Select(p => p.Name)
                    .Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)
            )
            .ToArray();
        return initializeProperties;
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
        if (BuildInDirectlyMappedTypes.Contains(type.Name))
        {
            sourceWriter.Write("reader.Get");
            sourceWriter.Write(GetReaderGetSuffix(type));
            sourceWriter.Write("OrNull(ordinalReader.Read()");
            sourceWriter.Write(")");
        }
        else
        {
            sourceWriter.Write(GetReaderVariableName(type));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
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
        sourceWriter.WriteLine("using System.Collections.Immutable;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.Services;");
        sourceWriter.WriteLine("using Cooke.Gnissel.SourceGeneration;");
        sourceWriter.WriteLine();
        WriteDbContextClassStart(sourceWriter, dbContextType);
        sourceWriter.WriteLine(" {");
        sourceWriter.Indent++;
        sourceWriter.WriteLine("public static partial class ObjectReaders {");
        sourceWriter.Indent++;
    }

    private static string GetDbContextIdentifierName(ITypeSymbol dbContextType)
    {
        var sb = new StringBuilder();
        AppendName(dbContextType);
        return sb.ToString();

        void AppendName(ITypeSymbol symbol)
        {
            if (symbol.ContainingType == null)
            {
                sb.Append(symbol.Name);
            }
            else
            {
                AppendName(symbol.ContainingType);
                sb.Append(".");
                sb.Append(symbol.Name);
            }
        }
    }

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

        WriteDbContextClassStart(sourceWriter, dbContextType);
        sourceWriter.WriteLine("(DbOptions options) : DbContext(options) {");
        sourceWriter.Indent++;
        sourceWriter.Write("public ");
        sourceWriter.Write(dbContextType.Name);
        sourceWriter.WriteLine(
            " (IDbAdapter adapter) : this(new DbOptions(adapter, ObjectReaders.CreateProvider(adapter))) { }"
        );
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");

        WriteDbContextClassEnd(sourceWriter, dbContextType);
    }

    private static void WriteDbContextClassStart(
        IndentedTextWriter sourceWriter,
        ITypeSymbol dbContextType
    )
    {
        var containingType = dbContextType.ContainingType;
        while (containingType != null)
        {
            sourceWriter.Write("public partial class ");
            sourceWriter.Write(containingType.Name);
            sourceWriter.WriteLine(" {");
            sourceWriter.Indent++;
            containingType = containingType.ContainingType;
        }
        sourceWriter.Write(
            $"{AccessibilityToString(dbContextType.DeclaredAccessibility)} partial class "
        );
        sourceWriter.Write(dbContextType.Name);
    }

    private static void WriteDbContextClassEnd(
        IndentedTextWriter sourceWriter,
        ITypeSymbol dbContextType
    )
    {
        var containingType = dbContextType.ContainingType;
        while (containingType != null)
        {
            sourceWriter.Indent--;
            sourceWriter.WriteLine("}");
            containingType = containingType.ContainingType;
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

    private static void WritePartialReaderClassEnd(
        IndentedTextWriter sourceWriter,
        ITypeSymbol dbContextType
    )
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");

        WriteDbContextClassEnd(sourceWriter, dbContextType);
    }

    private bool IsNullableValueTypeOrReferenceType(ITypeSymbol type) =>
        IsNullableValueType(type) || type.IsReferenceType;

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type is { Name: "Nullable" };
    }

    private static readonly IImmutableSet<string> BuildInDirectlyMappedTypes =
        ImmutableHashSet.Create("Int32", "String");

    private static readonly IImmutableSet<string> BuildInIndirectlyMappedTypes =
        ImmutableHashSet.Create("DateTime", "TimeSpan");

    private static readonly IImmutableSet<string> BuildInTypes = BuildInDirectlyMappedTypes
        .Union(BuildInIndirectlyMappedTypes)
        .ToImmutableHashSet();

    private static bool IsBuildIn(ITypeSymbol readTypeType) =>
        BuildInTypes.Contains(readTypeType.Name);

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

    private enum EnumMappingTechnique
    {
        Direct,
        String,
        Value,
    }

    private record DbContextOptions(EnumMappingTechnique EnumMappingTechnique)
    {
        public EnumMappingTechnique EnumMappingTechnique { get; } = EnumMappingTechnique;
    }
}
