using System.Text;

namespace Cooke.Gnissel;

public class SnakeCaseDbNameProvider : IDbNameProvider
{
    public string ToColumnName(string memberName) => GetSnakeCaseName(memberName);

    public string ToTableName(string typeName) => GetSnakeCaseName(typeName) + "s";

    private static string GetSnakeCaseName(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c))
            {
                if (sb.Length > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLower(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
