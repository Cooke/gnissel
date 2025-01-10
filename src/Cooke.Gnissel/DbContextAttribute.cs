namespace Cooke.Gnissel;

[AttributeUsage(AttributeTargets.Class)]
public class DbContextAttribute : Attribute
{
    public EnumMappingTechnique EnumMappingTechnique { get; init; } = EnumMappingTechnique.Direct;
}

public enum EnumMappingTechnique
{
    Direct,
    String,
    Value,
}
