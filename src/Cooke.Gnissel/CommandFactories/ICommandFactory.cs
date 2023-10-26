using System.Data.Common;

namespace Cooke.Gnissel;

public interface ICommandFactory
{
    DbCommand CreateCommand();
}