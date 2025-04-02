namespace Cooke.Gnissel;

public class DbMapAttribute : Attribute
{
    public MappingTechnique Technique { get; init; }
}
