using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public static class Extensions
{
    public static string ToNullableDisplayString(this ITypeSymbol type) =>
        type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated
            ? type.ToDisplayString() + "?"
            : type.ToDisplayString();
}
