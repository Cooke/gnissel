namespace Cooke.Gnissel;

public interface IDbNameProvider
{
    string ToColumnName(string memberName);

    string ToTableName(string typeName);

    string CombineColumnNames(string parentMemberColumnName, string childMemberColumnName) =>
        childMemberColumnName;
}
