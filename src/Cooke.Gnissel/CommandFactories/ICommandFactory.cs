using System.Data.Common;

namespace Cooke.Gnissel.CommandFactories;

public interface ICommandFactory
{
    DbCommand CreateCommand();
}