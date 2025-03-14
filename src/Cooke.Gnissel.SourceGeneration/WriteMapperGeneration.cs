namespace Cooke.Gnissel.SourceGeneration;

public static class ReadGenerator
{
    private static void WritePartialReadMappersClassStart(
        MappersClass mappersClass,
        IndentedTextWriter sourceWriter
    )
    {
        WritePartialMappersClassStart(mappersClass, sourceWriter);
        sourceWriter.WriteLine("public static partial class ReadMappers {");
        sourceWriter.Indent++;
    }
}
