using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public partial class Generator
{
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
                INamedTypeSymbol { Name: "Nullable" } nullableType => GetBaseName(
                    nullableType.TypeArguments[0]
                ) + "Nullable",

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

    private static bool IsAccessibleDeep(Mapping mapping, MappersClass mappersClass) =>
        FindAllMappings(mapping).All(t => IsAccessible(t.Type, mappersClass.Symbol));

    private static bool IsAccessible(ITypeSymbol typeSymbol, INamedTypeSymbol mappersClass)
    {
        return typeSymbol.DeclaredAccessibility != Accessibility.Private
            || SymbolEqualityComparer.Default.Equals(
                typeSymbol.ContainingType,
                mappersClass.ContainingType
            );
    }

    private static ITypeSymbol AdjustNull(ITypeSymbol type)
    {
        return type switch
        {
            INamedTypeSymbol { Name: "Nullable", TypeArguments.Length: 1 } nullable =>
                nullable.TypeArguments[0],

            INamedTypeSymbol { IsTupleType: true } tupleType => tupleType.ConstructedFrom.Construct(
                tupleType
                    .TupleElements.Select(x => x.Type.IsReferenceType ? AdjustNull(x.Type) : x.Type)
                    .ToArray()
            ),

            { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated } =>
                type.WithNullableAnnotation(NullableAnnotation.Annotated),

            _ => type,
        };
    }

    private static IEnumerable<Mapping> FindAllMappings(Mapping mapping)
    {
        yield return mapping;

        if (IsBuildIn(mapping.Type))
        {
            yield break;
        }

        if (mapping.Technique != MappingTechnique.Default)
        {
            yield break;
        }

        var ctorParameters = GetCtorParametersOrNull(mapping.Type);
        if (ctorParameters == null)
        {
            yield break;
        }

        foreach (var t in ctorParameters)
        {
            foreach (var innerType in FindAllMappings(CreateMapping(t.Parameter.Type)))
            {
                yield return innerType;
            }
        }
    }

    private static MappingTechnique GetMapTechnique(ITypeSymbol type) =>
        CreateMapping(type).Technique;

    private static Mapping CreateMapping(ITypeSymbol type)
    {
        var mapAttribute = type.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name == "DbMapAttribute");

        return CreateMapping(type, mapAttribute);
    }

    private static Mapping CreateMapping(ITypeSymbol type, AttributeData? mapAttribute)
    {
        var technique = MappingTechnique.Default;
        string? dbDataType = null;

        if (mapAttribute != null)
        {
            foreach (var argument in mapAttribute.NamedArguments)
            {
                switch (argument.Key)
                {
                    case "Technique":
                        technique = (MappingTechnique)argument.Value.Value!;
                        break;

                    case "DbTypeName":
                        dbDataType = (string?)argument.Value.Value;
                        break;
                }
            }
        }
        else if (
            !IsBuildIn(type)
            && !type.IsTupleType
            && type.TypeKind != TypeKind.Enum
            && GetCtorParametersOrNull(type) == null
        )
        {
            technique = MappingTechnique.Custom;
        }
        else
        {
            technique = MappingTechnique.Default;
        }

        return new Mapping(AdjustNull(type), technique, dbDataType);
    }

    private static string GetCreateReaderDescriptorsName(ITypeSymbol type) =>
        $"Create{GetTypeIdentifierName(AdjustNull(type))}Descriptors";

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

    private static bool IsBuildIn(ITypeSymbol type) =>
        GetRootNamespace(type)?.StartsWith("System") == true && !type.IsTupleType;

    private static string? GetRootNamespace(ITypeSymbol typeSymbol)
    {
        var containingNamespace = typeSymbol.ContainingNamespace;
        while (containingNamespace.ContainingNamespace is { IsGlobalNamespace: false })
        {
            containingNamespace = containingNamespace.ContainingNamespace;
        }

        return containingNamespace?.Name;
    }

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

                    case "NamingConvention":
                        NamingConvention = (NamingConvention)argument.Value.Value!;
                        break;
                }
            }

            MappingsByAttributes = symbol
                .GetAttributes()
                .Where(x =>
                    x.AttributeClass?.Name == "DbMapAttribute" && x.ConstructorArguments.Length == 1
                )
                .Select(attribute =>
                {
                    var type = (INamedTypeSymbol)attribute.ConstructorArguments.First().Value!;
                    return CreateMapping(type, attribute);
                })
                .ToImmutableArray();
        }

        public ImmutableArray<Mapping> MappingsByAttributes { get; }

        public INamedTypeSymbol Symbol { get; }

        public MappingTechnique EnumMappingTechnique { get; } = MappingTechnique.AsIs;

        public NamingConvention NamingConvention { get; } = NamingConvention.AsIs;
    }

    private enum NamingConvention
    {
        AsIs,
        SnakeCase,
    }

    private record Mapping(ITypeSymbol Type, MappingTechnique Technique, string? DbDataType)
    {
        public MappingTechnique Technique { get; } = Technique;

        public string? DbDataType { get; } = DbDataType;

        public ITypeSymbol Type { get; } = Type;

        private sealed class TechniqueDbDataTypeTypeEqualityComparer : IEqualityComparer<Mapping>
        {
            public bool Equals(Mapping? x, Mapping? y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x is null)
                    return false;
                if (y is null)
                    return false;
                if (x.GetType() != y.GetType())
                    return false;
                return x.Technique == y.Technique
                    && x.DbDataType == y.DbDataType
                    && SymbolEqualityComparer.Default.Equals(x.Type, y.Type);
            }

            public int GetHashCode(Mapping obj)
            {
                unchecked
                {
                    var hashCode = (int)obj.Technique;
                    hashCode =
                        (hashCode * 397)
                        ^ (obj.DbDataType != null ? obj.DbDataType.GetHashCode() : 0);
                    hashCode =
                        (hashCode * 397) ^ SymbolEqualityComparer.Default.GetHashCode(obj.Type);
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<Mapping> TechniqueDbDataTypeTypeComparer { get; } =
            new TechniqueDbDataTypeTypeEqualityComparer();
    }

    private enum MappingTechnique
    {
        Default,
        AsIs,
        AsString,
        AsInteger,
        Custom,
    }

    private static string? GetColumnName(
        MappersClass mappersClass,
        ISymbol symbol1,
        ISymbol? symbol2 = null
    )
    {
        var nameAttribute =
            symbol1.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbNameAttribute")
            ?? symbol2
                ?.GetAttributes()
                .FirstOrDefault(x => x.AttributeClass?.Name == "DbNameAttribute");
        if (nameAttribute != null)
        {
            return nameAttribute.ConstructorArguments.First().Value as string;
        }

        return null;
    }

    private static string GetSnakeCaseName(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c))
            {
                if (sb.Length > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLower(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
