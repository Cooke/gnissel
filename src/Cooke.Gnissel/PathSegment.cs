namespace Cooke.Gnissel;

public abstract record PathSegment
{
    public static PathSegment Combine(PathSegment? parent, PathSegment child) =>
        parent is null ? child : new NestedPathSegment(parent, child);

    public IEnumerable<string> ToStringEnumrable()
    {
        switch (this)
        {
            case ParameterPathSegment parameterPathSegment:
                yield return parameterPathSegment.Name;
                break;
            case PropertyPathSegment propertyPathSegment:
                yield return propertyPathSegment.Name;
                break;
            case NestedPathSegment nestedPathSegment:
                foreach (var part in nestedPathSegment.Parent.ToStringEnumrable())
                {
                    yield return part;
                }

                foreach (var part in nestedPathSegment.Child.ToStringEnumrable())
                {
                    yield return part;
                }

                break;
        }
    }
}

public record ParameterPathSegment(string Name) : PathSegment;

public record PropertyPathSegment(string Name) : PathSegment;

public record NestedPathSegment(PathSegment Parent, PathSegment Child) : PathSegment;
