namespace Cooke.Gnissel.Test;

public partial class SourceGeneration
{
    public partial class GeneratedObjectReaderProvider
    {
        private readonly ObjectReader<Device> _deviceReader = CreateObjectReader<Device>(
            adapter,
            (reader, columnOrdinals) =>
                new(
                    reader.GetString(
                        columnOrdinals[0] /* name */
                    )
                ),
            [new ParameterPathSegment("name")]
        );
    }
}