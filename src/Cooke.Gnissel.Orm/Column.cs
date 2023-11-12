#region

using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel;

public class Column<TTable>
{
    private readonly Func<TTable, DbParameter> _parameterFactory;

    public Column(string name, bool isIdentity, Func<TTable, DbParameter> parameterFactory)
    {
        _parameterFactory = parameterFactory;
        Name = name;
        IsIdentity = isIdentity;
    }

    public DbParameter CreateParameter(TTable item) => _parameterFactory(item);

    public string Name { get; }

    public bool IsIdentity { get; }

    public MemberInfo Member { get; }
}
