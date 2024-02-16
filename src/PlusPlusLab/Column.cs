using System.Data.Common;
using System.Reflection;

namespace PlusPlusLab;

public class Column<TTable>(
    Table<TTable> table,
    string name,
    bool isIdentity,
    MemberInfo member,
    Func<TTable, DbParameter> parameterFactory)
    : IColumn
{

    public ITable Table => table;

    public string Name { get; } = name;

    public bool IsIdentity { get; } = isIdentity;

    public MemberInfo Member { get; } = member;

    public DbParameter CreateParameter(TTable item)
    {
        return parameterFactory(item);
    }
}