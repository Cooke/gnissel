using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers : IMapperProvider
{
    internal partial class DbReaders
    {
        public IEnumerable<IObjectReader> CreateAnonymousReaders() =>
            [
                IObjectReader.Create(
                    (reader, ordinalReader) =>
                    {
                        var name = reader.GetValueOrNull<string>(ordinalReader.Read());
                        if (name is null)
                        {
                            return null;
                        }

                        return new { Name = name };
                    },
                    () =>

                        [
                            .. StringReader.ReadDescriptors.Select(d =>
                                d.WithParent(NameProvider, "Name")
                            ),
                        ]
                ),
            ];
    }
}
