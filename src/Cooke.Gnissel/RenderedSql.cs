using System.Data.Common;

namespace Cooke.Gnissel;

public record RenderedSql(string CommandText, DbParameter[] Parameters);
