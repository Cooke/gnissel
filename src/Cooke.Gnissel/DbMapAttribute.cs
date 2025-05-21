namespace Cooke.Gnissel;

public class DbMapAttribute : Attribute
{
    public DbMapAttribute() { }

    public DbMapAttribute(Type typ) { }

    public MappingTechnique Technique { get; init; }

    public string? DbTypeName { get; init; }
}
