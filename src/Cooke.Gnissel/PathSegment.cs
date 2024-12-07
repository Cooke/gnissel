namespace Cooke.Gnissel;

public abstract record PathSegment
{
    public static PathSegment Combine(PathSegment? parent, PathSegment child) =>
        parent is null ? child : new NestedPathSegment(parent, child);
}

public record ParameterPathSegment(string Name) : PathSegment;

public record PropertyPathSegment(string Name) : PathSegment;

public record NestedPathSegment(PathSegment Parent, PathSegment Child) : PathSegment;
