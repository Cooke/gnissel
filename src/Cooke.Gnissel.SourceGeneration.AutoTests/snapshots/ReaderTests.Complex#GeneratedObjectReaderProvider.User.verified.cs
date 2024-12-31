//HintName: GeneratedObjectReaderProvider.User.cs
namespace Gnissel.SourceGeneration;

using System.Data.Common;
using Cooke.Gnissel;
using Cooke.Gnissel.SourceGeneration;
using System.Collections.Immutable;
using Cooke.Gnissel.Services;

public partial class GeneratedObjectReaderProvider
{
    private readonly ObjectReader<Cooke.Gnissel.SourceGeneration.Test.Program.User> _userReader;
    
    private static MultiReaderMetadata ReadUserMetadata => new ([
        new NameReaderMetadata("name"),
        new NameReaderMetadata("age"),
        new NameReaderMetadata("size")]);
    
    public global::Cooke.Gnissel.SourceGeneration.Test.Program.User ReadUser(DbDataReader reader, OrdinalReader ordinalReader)
    {
        var name = reader.GetStringOrNull(ordinalReader.Read());
        var age = reader.GetInt32OrNull(ordinalReader.Read());
        var size = reader.GetInt32OrNull(ordinalReader.Read());
        
        if (name is null && age is null && size is null)
        {
            return null;
        }
        
        return new global::Cooke.Gnissel.SourceGeneration.Test.Program.User(
            name ?? throw new InvalidOperationException("Expected non-null value"),
            age ?? throw new InvalidOperationException("Expected non-null value"),
            size);
    }
}
