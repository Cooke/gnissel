using System.Data.Common;
using System.Reflection;

namespace Cooke.Gnissel.Typed;

public interface IColumn
{
    string Name { get; }

    MemberInfo Member { get; }
    
    bool IsDatabaseGenerated { get; }
}


public class Column<TTable>(
    string name,
    MemberInfo member,
    Func<TTable, DbParameter> parameterFactory,
    bool isDatabaseGenerated)
    : IColumn
{
    public string Name { get; } = name;

    public MemberInfo Member { get; } = member;

    public bool IsDatabaseGenerated { get; } = isDatabaseGenerated;

    public DbParameter CreateParameter(TTable item)
    {
        return parameterFactory(item);
    }
}