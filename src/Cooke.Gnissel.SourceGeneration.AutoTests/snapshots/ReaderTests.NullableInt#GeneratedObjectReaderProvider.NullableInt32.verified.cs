//HintName: GeneratedObjectReaderProvider.NullableInt32.cs
namespace Gnissel.SourceGeneration;

using System.Data.Common;
using Cooke.Gnissel;
using Cooke.Gnissel.SourceGeneration;
using System.Collections.Immutable;
using Cooke.Gnissel.Services;

public partial class GeneratedObjectReaderProvider
{
    private readonly ObjectReader<int?> _nullableInt32Reader;
    
    private static readonly NextOrdinalReaderMetadata ReadNullableInt32Metadata = new ();
    
    public int? ReadNullableInt32(DbDataReader reader, OrdinalReader ordinalReader)
    {
        return reader.GetInt32OrNull(ordinalReader.Read());
    }
}
