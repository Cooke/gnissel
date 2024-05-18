namespace Cooke.Gnissel;

public abstract class DbConverterFactory : DbConverter
{
    public abstract bool CanCreateFor(Type type);

    public abstract DbConverter Create(Type type);
}
