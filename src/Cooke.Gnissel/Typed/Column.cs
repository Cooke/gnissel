using System.Data.Common;
using System.Reflection;

namespace Cooke.Gnissel.Typed;

public interface IColumn
{
    string Name { get; }

    IReadOnlyCollection<MemberInfo> MemberChain { get; }

    bool IsDatabaseGenerated { get; }
}

public class Column<TTable>(
    string name,
    IReadOnlyCollection<MemberInfo> memberChain,
    Func<TTable, DbParameter> parameterFactory,
    bool isDatabaseGenerated
) : IColumn
{
    public string Name { get; } = name;

    public IReadOnlyCollection<MemberInfo> MemberChain { get; } = memberChain;

    public bool IsDatabaseGenerated { get; } = isDatabaseGenerated;

    public DbParameter CreateParameter(TTable item) => parameterFactory(item);
}
