using System.Data.Common;
using System.Reflection;

namespace PlusPlusLab;

public class Column<TTable>(
    string name,
    MemberInfo member,
    Func<TTable, DbParameter> parameterFactory)
    : IColumn
{
    public string Name { get; } = name;

    public MemberInfo Member { get; } = member;

    public DbParameter CreateParameter(TTable item)
    {
        return parameterFactory(item);
    }
}