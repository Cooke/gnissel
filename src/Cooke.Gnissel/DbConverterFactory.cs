namespace Cooke.Gnissel;

public abstract class DbConverterFactory : IDbConverter
{
    public abstract bool CanCreateFor(Type type);

    public abstract IDbConverter Create(Type type);
}
