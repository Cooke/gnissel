using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
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
                            .SelectMany(FindAllReadTypes)
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
                WritePartialReadMappersClassStart(mappersClass, sourceWriter);
                sourceWriter.WriteLine();
                WriteReaderMetadata(sourceWriter, type);
                WriteObjectReaderDescriptorField(sourceWriter, type);

                if (type.IsValueType)
                {
                    WriteNotNullableObjectReaderDescriptorField(sourceWriter, type);
                    WriteNotNullableReadMethod(sourceWriter, type);
                }

                WriteCreateReadMethodStart(sourceWriter, type);
                WriteReaderBody(type, mappersClass, sourceWriter);
                WriteCreateReadMethodEnd(sourceWriter);
                WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
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
                WritePartialReadMappersClassStart(mappersClass, sourceWriter);

                sourceWriter.WriteLine(
                    "public static ImmutableArray<IObjectReaderDescriptor> GetReadDescriptors() => ["
                );
                sourceWriter.Indent++;

                for (var index = 0; index < types.Length; index++)
                {
                    var type = types[index];
                    sourceWriter.Write("ReadMappers.");
                    sourceWriter.Write(GetObjectReaderDescriptorFieldName(type));
                    if (type.IsValueType)
                    {
                        sourceWriter.WriteLine(",");
                        sourceWriter.Write("ReadMappers.");
                        sourceWriter.Write(GetNotNullableObjectReaderDescriptorFieldName(type));
                    }

                    if (index < types.Length - 1)
                    {
                        sourceWriter.WriteLine(",");
                    }
                }

                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");

                WritePartialReadMappersClassEnd(mappersClass, sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.ReadMappers.cs",
                    stringWriter.ToString()
                );
            }
        );

        initContext.RegisterImplementationSourceOutput(
            mappersPipeline,
            (context, mappersClass) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialMappersClassStart(mappersClass, sourceWriter);
                sourceWriter.WriteLine();

                sourceWriter.WriteLine(
                    "public static readonly ImmutableArray<IObjectReaderDescriptor> AllDescriptors;"
                );
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"static {mappersClass.Symbol.Name}() {{");
                sourceWriter.Indent++;
                sourceWriter.WriteLine("AllDescriptors = [");
                sourceWriter.Indent++;

                sourceWriter.WriteLine("..GetReadDescriptors(),");
                sourceWriter.WriteLine("..GetWriteDescriptors()");

                sourceWriter.Indent--;
                sourceWriter.WriteLine("];");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();

                WritePartialMappersClassEnd(mappersClass, sourceWriter);
                sourceWriter.Flush();

                context.AddSource(
                    $"{GetSourceName(mappersClass.Symbol)}.cs",
                    stringWriter.ToString()
                );
            }
        );

        GenerateWriteMappers(initContext, mappersPipeline);
    }

    private static bool IsAccessibleDeep(ITypeSymbol typeSymbol, MappersClass mappersClass) =>
        FindAllReadTypes(typeSymbol).All(t => IsAccessible(t, mappersClass.Symbol));

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

    private static IEnumerable<ITypeSymbol> FindAllReadTypes(ITypeSymbol type)
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
                foreach (var innerType in FindAllReadTypes(t.Type))
                {
                    yield return innerType;
                }
            }
        }
    }

    private static string GetReaderMetadataName(ITypeSymbol type)
    {
        return $"{GetTypeIdentifierName(AdjustNulls(type))}ReaderMetadata";
    }

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

    private static void WriteCreateReadMethodEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("};");
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

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
        AsIs,
        AsString,
        AsInteger,
    }
}
