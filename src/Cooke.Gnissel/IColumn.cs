using System.Data.Common;
using System.Reflection;

namespace Cooke.Gnissel;

public interface IColumn<in TTable>
{
    public DbParameter CreateParameter(TTable item);
    string Name { get; }
    bool IsIdentity { get; }
    MemberInfo Member { get; }
}
