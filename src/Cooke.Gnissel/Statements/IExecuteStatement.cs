using System.Runtime.CompilerServices;
using Cooke.Gnissel.CommandFactories;

namespace Cooke.Gnissel.Statements;

public interface IExecuteStatement
{
    ValueTaskAwaiter<int> GetAwaiter();

    CompiledSql CompiledSql { get; }
}
