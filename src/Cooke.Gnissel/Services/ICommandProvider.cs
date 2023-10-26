using System.Data.Common;

namespace Cooke.Gnissel;

public interface ICommandProvider
{
    DbCommand CreateCommand();
}