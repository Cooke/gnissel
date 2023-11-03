using System.Data.Common;

namespace Cooke.Gnissel;

public record CompiledSql(string CommandText, DbParameter[] Parameters);