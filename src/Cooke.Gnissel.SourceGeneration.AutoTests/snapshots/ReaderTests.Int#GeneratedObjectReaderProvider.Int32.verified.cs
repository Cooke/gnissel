//HintName: GeneratedObjectReaderProvider.Int32.cs
namespace Gnissel.SourceGeneration;

using System.Data.Common;
using Cooke.Gnissel;
using Cooke.Gnissel.SourceGeneration;
using System.Collections.Immutable;
using Cooke.Gnissel.Services;

public partial class GeneratedObjectReaderProvider
{
    private readonly ObjectReader<int> _int32Reader;
    
    private static readonly NextOrdinalReaderMetadata ReadInt32Metadata = new ();
    
    public int ReadInt32(DbDataReader reader, OrdinalReader ordinalReader)
    {
        return reader.GetInt32OrNull(ordinalReader.Read());
    }
}
