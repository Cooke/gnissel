//HintName: GeneratedObjectReaderProvider.User.cs
namespace Gnissel.SourceGeneration;

using System.Data.Common;

public partial class GeneratedObjectReaderProvider
{
    public global::Cooke.Gnissel.SourceGeneration.Test.Program.User ReadUser(DbDataReader reader, IReadOnlyList<int> columnOrdinals)
    {
        return new global::Cooke.Gnissel.SourceGeneration.Test.Program.User(
            reader.GetString(columnOrdinals[0]) /* name */,
            reader.GetInt32(columnOrdinals[1]) /* age */);
    }
}
