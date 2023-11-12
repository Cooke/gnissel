#region

using System.Data.Common;
using System.Reflection;

#endregion

namespace Cooke.Gnissel.Services;

public interface IDbAdapter
{
    string EscapeIdentifier(string identifier);

    DbParameter CreateParameter<TValue>(TValue value, string? dbType = null);

    RenderedSql RenderSql(Sql sql);

    DbBatchCommand CreateBatchCommand();
    
    DbCommand CreateCommand();
    
    IDbConnector CreateConnector();
    
    IIdentifierMapper DefaultIdentifierMapper { get; }
}
