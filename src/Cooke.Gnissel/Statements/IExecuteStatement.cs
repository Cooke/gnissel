using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;

namespace Cooke.Gnissel.Statements;

public interface IExecuteStatement
{
    ValueTask<int> ExecuteAsync(
        ICommandFactory? commandFactory = null,
        CancellationToken cancellationToken = default
    );

    ValueTaskAwaiter<int> GetAwaiter();
}
