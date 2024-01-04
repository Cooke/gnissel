#region

using System.Data.Common;
using System.Reflection;

#endregion

namespace Cooke.Gnissel.PlusPlus;

public interface IColumn
{
    string Name { get; }
    MemberInfo Member { get; }
    ITable Table { get; }
}

public class Column<TTable> : IColumn
{
    private readonly Table<TTable> _table;
    private readonly Func<TTable, DbParameter> _parameterFactory;

    public Column(
        Table<TTable> table,
        string name,
        bool isIdentity,
        MemberInfo member,
        Func<TTable, DbParameter> parameterFactory
    )
    {
        _table = table;
        _parameterFactory = parameterFactory;
        Name = name;
        IsIdentity = isIdentity;
        Member = member;
    }

    public ITable Table => _table;

    public string Name { get; }

    public bool IsIdentity { get; }

    public MemberInfo Member { get; }

    public DbParameter CreateParameter(TTable item)
    {
        return _parameterFactory(item);
    }
}
