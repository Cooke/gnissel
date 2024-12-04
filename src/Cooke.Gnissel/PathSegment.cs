namespace Cooke.Gnissel;

public abstract record PathSegment;

public record ParameterPathSegment(string Name) : PathSegment;

public record PropertyPathSegment(string Name) : PathSegment;

public record NestedPathSegment(PathSegment Parent, PathSegment Child) : PathSegment;
