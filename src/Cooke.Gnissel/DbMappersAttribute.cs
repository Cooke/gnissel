namespace Cooke.Gnissel;

[AttributeUsage(AttributeTargets.Class)]
public class DbMappersAttribute : Attribute
{
    public MappingTechnique EnumMappingTechnique { get; init; } = MappingTechnique.AsIs;
}
