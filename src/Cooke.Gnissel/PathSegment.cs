using System.Reflection;

namespace Cooke.Gnissel;

public abstract record PathSegment;

public record ParameterPathSegment(ParameterInfo ParameterInfo) : PathSegment;

public record PropertyPathSegment(PropertyInfo PropertyInfo) : PathSegment;
