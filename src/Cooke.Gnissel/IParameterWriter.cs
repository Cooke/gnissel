namespace Cooke.Gnissel;

public interface IParameterWriter
{
    void Write<T>(T value, string? dbType = null);
}
