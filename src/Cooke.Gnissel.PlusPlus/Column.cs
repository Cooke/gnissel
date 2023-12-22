#region

using System.Data.Common;
using System.Reflection;

#endregion

namespace Cooke.Gnissel.PlusPlus;

public interface IColumn
{
    string Name { get; }
    MemberInfo Member { get; }
}

public class Column<TTable> : IColumn
{
    private readonly Func<TTable, DbParameter> _parameterFactory;

    public Column(
        string name,
        bool isIdentity,
        MemberInfo member,
        Func<TTable, DbParameter> parameterFactory
    )
    {
        _parameterFactory = parameterFactory;
        Name = name;
        IsIdentity = isIdentity;
        Member = member;
    }

    public string Name { get; }

    public bool IsIdentity { get; }

    public MemberInfo Member { get; }

    public DbParameter CreateParameter(TTable item)
    {
        return _parameterFactory(item);
    }
}
