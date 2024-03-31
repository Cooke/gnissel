using System.Reflection;

namespace Cooke.Gnissel.Services;

public abstract record ObjectPathPart;

public record ParameterPathPart(ParameterInfo ParameterInfo) : ObjectPathPart;

public record PropertyPathPart(PropertyInfo PropertyInfo) : ObjectPathPart;
