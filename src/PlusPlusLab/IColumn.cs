using System.Reflection;

namespace PlusPlusLab;

public interface IColumn
{
    string Name { get; }
    MemberInfo Member { get; }
    ITable Table { get; }
}
