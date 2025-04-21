using Cooke.Gnissel;

namespace Gnissel.SourceGeneration;

internal partial class DbMappers
{
    internal partial class DbWriters
    {
        public ObjectWriter<Address?> AddressWriter { get; init; }

        private void WriteAddress(Address? value, IParameterWriter parameterWriter)
        {
            if (value is null)
            {
                parameterWriter.Write<string?>(null);
                return;
            }

            parameterWriter.Write(value.Street);
        }
    }
}
