namespace Cooke.Gnissel;

public class DefaultDbNameProvider : IDbNameProvider
{
    public string ToColumnName(string memberName) => memberName;

    public string ToTableName(string typeName) => typeName + "s";
}
