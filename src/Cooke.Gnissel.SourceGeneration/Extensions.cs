using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

public static class Extensions
{
    public static string ToNullableDisplayString(this ITypeSymbol type) =>
        type switch
        {
            { IsReferenceType: true, NullableAnnotation: not NullableAnnotation.Annotated } =>
                type.ToDisplayString() + "?",
            { IsValueType: true, Name: not "Nullable" } => type.ToDisplayString() + "?",
            _ => type.ToDisplayString(),
        };
}
