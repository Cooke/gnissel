namespace Cooke.Gnissel;

public class DefaultDbNameProvider : IDbNameProvider
{
    public string ToColumnName(IEnumerable<string> memberNameChain) => memberNameChain.Last();

    public string ToTableName(string typeName) => typeName + "s";
}
