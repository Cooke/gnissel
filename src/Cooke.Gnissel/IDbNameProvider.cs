namespace Cooke.Gnissel;

public interface IDbNameProvider
{
    string ToColumnName(IEnumerable<string> memberNameChain);

    string ToTableName(string typeName);
}
