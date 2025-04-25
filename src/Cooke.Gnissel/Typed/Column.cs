using System.Reflection;

namespace Cooke.Gnissel.Typed;

public interface IColumn
{
    string Name { get; }

    IReadOnlyCollection<MemberInfo> MemberChain { get; }
}

public class Column<TTable>(string name, IReadOnlyCollection<MemberInfo> memberChain) : IColumn
{
    public string Name { get; } = name;

    public IReadOnlyCollection<MemberInfo> MemberChain { get; } = memberChain;
}
