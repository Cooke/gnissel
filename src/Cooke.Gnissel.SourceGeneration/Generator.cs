using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // var selectTypesPipeline = initContext
        //     .SyntaxProvider.CreateSyntaxProvider(
        //         (node, _) =>
        //             node
        //                 is InvocationExpressionSyntax
        //                 {
        //                     Expression: MemberAccessExpressionSyntax
        //                     {
        //                         Name.Identifier.ValueText: "Select"
        //                     },
        //                     ArgumentList.Arguments.Count: 1
        //                 },
        //         (context, ct) =>
        //         {
        //             var invocation = (InvocationExpressionSyntax)context.Node;
        //             var operation = context.SemanticModel.GetOperation(invocation, ct);
        //             if (
        //                 operation
        //                 is not IInvocationOperation
        //                 {
        //                     TargetMethod: { TypeArguments: { Length: 1 } typeArguments }
        //                 }
        //             )
        //             {
        //                 return null;
        //             }
        //
        //             var typeArg = typeArguments[0];
        //             return typeArg;
        //         }
        //     )
        //     .Where(x => x != null)
        //     .Select((v, _) => v!);

        var mappersPipeline = initContext.SyntaxProvider.ForAttributeWithMetadataName(
            "Cooke.Gnissel.DbMappersAttribute",
            (_, _) => true,
            (context, _) => new MappersClass((INamedTypeSymbol)context.TargetSymbol)
        );

        var queryTypesPipeline = initContext
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

                    // TODO check that instance type is a DbContext

                    var genericName = (GenericNameSyntax)memberAccess.Name;
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    var typeArgInfo = context.SemanticModel.GetTypeInfo(typeArg);
                    if (typeArgInfo.Type is null)
                    {
                        return null;
                    }

                    return typeArgInfo.Type;
                }
            )
            .Where(type => type != null)
            .Select((input, _) => input!)
            // Currently indirect usage is not supported (unbound type parameters)
            .Where(type => type is not ITypeParameterSymbol)
            .Collect();

        var mapperClassesWithTypesPipeline = mappersPipeline
            .Combine(queryTypesPipeline)
            .Select(
                (pair, _) =>
                {
                    var mappersClass = pair.Left;
                    var types = pair.Right;
                    return (
                        mappersClass,
                        types: types
                            .Where(t => IsAccessibleDeep(t, mappersClass))
                            .SelectMany(FindAllTypes)
                            .Select(AdjustNulls)
                            .Distinct(SymbolEqualityComparer.Default)
                            .Cast<ITypeSymbol>()
                            .ToArray()
                    );
                }
            );

        initContext.RegisterImplementationSourceOutput(
            mapperClassesWithTypesPipeline.SelectMany(
                (mappersClassWithTypes, _) =>
                    mappersClassWithTypes.types.Select(t => new ReadTypeWithMappersClass(
                        t,
                        mappersClassWithTypes.mappersClass
                    ))
            ),
            (context, readTypeWithMappersClass) =>
            {
                var type = readTypeWithMappersClass.Type;
                var mappersClass = readTypeWithMappersClass.MappersClass;

                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                GenerateReader(mappersClass, sourceWriter, type);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        initContext.RegisterImplementationSourceOutput(
            mapperClassesWithTypesPipeline,
            (context, mapperWithTypes) =>
            {
                var types = mapperWithTypes.types;
                var mappersClass = mapperWithTypes.mappersClass;
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialMappersClassStart(mappersClass, sourceWriter);

                sourceWriter.WriteLine(
                    types.Any(NeedCustomReader)
                        ? "public required DbReaders Readers { get; init; }"
                        : "public DbReaders Readers { get; init; } = new DbReaders();"
                );
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public IObjectReaderProvider ReaderProvider => Readers;");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public partial class DbReaders : IObjectReaderProvider {");
                sourceWriter.Indent++;

                sourceWriter.WriteLine("private IObjectReaderProvider? _readerProvider;");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public DbReaders() { ");
                sourceWriter.Indent++;
                foreach (var type in types)
                {
                    if (!NeedCustomReader(type))
                    {
                        sourceWriter.Write(GetReaderPropertyName(type));
                        sourceWriter.Write(" = new ObjectReader<");
                        sourceWriter.Write(type.ToDisplayString());
                        if (
                            type is
                            {
                                IsReferenceType: true,
                                NullableAnnotation: NullableAnnotation.NotAnnotated
                            }
                        )
                        {
                            sourceWriter.Write("?");
                        }
                        sourceWriter.Write(">(");
                        sourceWriter.Write(GetReadMethodName(type));
                        sourceWriter.Write(",");
                        sourceWriter.Write(GetCreateReaderDescriptorsName(type));
                        sourceWriter.WriteLine(");");

                        if (type.IsValueType)
                        {
                            sourceWriter.Write(GetNullableReaderPropertyName(type));
                            sourceWriter.Write(" = new ObjectReader<");
                            sourceWriter.Write(type.ToDisplayString());
                            sourceWriter.Write("?>(");
                            sourceWriter.Write(GetNullableReadMethodName(type));
                            sourceWriter.Write(",");
                            sourceWriter.Write(GetCreateReaderDescriptorsName(type));
                            sourceWriter.WriteLine(");");
                        }
                    }
                }
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public ImmutableArray<IObjectReader> GetAllReaders() => [");
                sourceWriter.Indent++;

                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write(GetReaderPropertyName(type));
                    if (type.IsValueType)
                    {
                        sourceWriter.WriteLine(",");
                        sourceWriter.Write(GetNullableReaderPropertyName(type));
                    }

                    if (index < types.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");
                sourceWriter.WriteLine();

                sourceWriter.WriteLine("public ObjectReader<TOut> Get<TOut>()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indent++;
                sourceWriter.WriteLine(
                    "_readerProvider ??= DictionaryObjectReaderProvider.From(GetAllReaders());"
                );
                sourceWriter.WriteLine("return _readerProvider.Get<TOut>();");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");

                WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.cs",
                    stringWriter.ToString()
                );
            }
        );

        GenerateWriteMappers(initContext, mappersPipeline);
    }

    private static bool IsAccessibleDeep(ITypeSymbol typeSymbol, MappersClass mappersClass) =>
        FindAllTypes(typeSymbol).All(t => IsAccessible(t, mappersClass.Symbol));

    private static bool IsAccessible(ITypeSymbol typeSymbol, INamedTypeSymbol mappersClass)
    {
        return typeSymbol.DeclaredAccessibility != Accessibility.Private
            || SymbolEqualityComparer.Default.Equals(
                typeSymbol.ContainingType,
                mappersClass.ContainingType
            );
    }

    private record ReadTypeWithMappersClass(ITypeSymbol Type, MappersClass MappersClass)
    {
        public ITypeSymbol Type { get; } = Type;

        public MappersClass MappersClass { get; } = MappersClass;
    }

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

    private static IEnumerable<ITypeSymbol> FindAllTypes(ITypeSymbol type)
    {
        yield return type;

        if (IsBuildIn(type))
        {
            yield break;
        }

        if (GetMapTechnique(type) != MappingTechnique.Default)
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
            foreach (var innerType in FindAllTypes(t.Type))
            {
                yield return innerType;
            }
        }
    }

    private static MappingTechnique GetMapTechnique(ITypeSymbol type)
    {
        var mapAttribute = type.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name == "DbMapAttribute");
        if (mapAttribute != null)
        {
            foreach (var argument in mapAttribute.NamedArguments)
            {
                switch (argument.Key)
                {
                    case "Technique":
                        return (MappingTechnique)argument.Value.Value!;
                }
            }
        }

        return MappingTechnique.Default;
    }

    private static string GetCreateReaderDescriptorsName(ITypeSymbol type) =>
        $"Create{GetTypeIdentifierName(AdjustNulls(type))}Descriptors";

    private static string? GetSourceName(ITypeSymbol? type)
    {
        if (type == null)
        {
            return null;
        }

        var baseName = GetSourceName(type.ContainingType);
        if (baseName != null)
        {
            return baseName + "." + type.Name;
        }

        return type.Name;
    }

    private static bool IsNullableValueType(ITypeSymbol type) => type is { Name: "Nullable" };

    private static readonly IImmutableSet<string> BuildInDirectlyMappedTypes =
        ImmutableHashSet.Create("Int32", "String");

    private static readonly IImmutableSet<string> BuildInIndirectlyMappedTypes =
        ImmutableHashSet.Create("DateTime", "TimeSpan");

    private static readonly IImmutableSet<string> BuildInTypes = BuildInDirectlyMappedTypes
        .Union(BuildInIndirectlyMappedTypes)
        .ToImmutableHashSet();

    private static bool IsBuildIn(ITypeSymbol readTypeType) =>
        BuildInTypes.Contains(readTypeType.Name);

    private class MappersClass
    {
        public MappersClass(INamedTypeSymbol symbol)
        {
            Symbol = symbol;

            var mappersAttribute = symbol
                .GetAttributes()
                .First(x => x.AttributeClass?.Name == "DbMappersAttribute");

            foreach (var argument in mappersAttribute.NamedArguments)
            {
                switch (argument.Key)
                {
                    case "EnumMappingTechnique":
                        EnumMappingTechnique = (MappingTechnique)argument.Value.Value!;
                        break;
                }
            }
        }

        public INamedTypeSymbol Symbol { get; }

        public MappingTechnique EnumMappingTechnique { get; } = MappingTechnique.AsIs;
    }

    private enum MappingTechnique
    {
        Default,
        AsIs,
        AsString,
        AsInteger,
    }
}
