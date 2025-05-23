﻿using System.CodeDom.Compiler;
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

        var ctorParameters = GetCtorParametersOrNull(type);
        if (ctorParameters == null)
        {
            yield break;
        }

        foreach (var t in ctorParameters)
        {
            foreach (var innerType in FindAllTypes(t.Parameter.Type))
            {
                yield return innerType;
            }
        }
    }

    private static MappingTechnique GetMapTechnique(ITypeSymbol type) =>
        GetMapOptions(type).Technique;

    private static MappingOptions GetMapOptions(ITypeSymbol type)
    {
        MappingTechnique technique = MappingTechnique.Default;
        string? dbDataType = null;

        var mapAttribute = type.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name == "DbMapAttribute");
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

        return new MappingOptions(technique, dbDataType);
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

                    case "NamingConvention":
                        NamingConvention = (NamingConvention)argument.Value.Value!;
                        break;
                }
            }
        }

        public INamedTypeSymbol Symbol { get; }

        public MappingTechnique EnumMappingTechnique { get; } = MappingTechnique.AsIs;

        public NamingConvention NamingConvention { get; } = NamingConvention.AsIs;
    }

    private enum NamingConvention
    {
        AsIs,
        SnakeCase,
    }

    private record MappingOptions(MappingTechnique Technique, string? DbDataType)
    {
        public MappingTechnique Technique { get; } = Technique;

        public string? DbDataType { get; } = DbDataType;
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

        switch (mappersClass.NamingConvention)
        {
            case NamingConvention.SnakeCase:
                return GetSnakeCaseName(symbol1.Name);
            case NamingConvention.AsIs:
                return symbol1.Name;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
