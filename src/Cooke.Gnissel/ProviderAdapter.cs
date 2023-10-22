using System.Data.Common;

namespace Cooke.Gnissel;

public interface ProviderAdapter
{
    string EscapeIdentifier(string identifier);

    DbParameter CreateParameter<TValue>(TValue value);

    DbCommand CreateCommand();
}
