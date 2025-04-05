using Microsoft.CodeAnalysis;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var mappersPipeline = initContext.SyntaxProvider.ForAttributeWithMetadataName(
            "Cooke.Gnissel.DbMappersAttribute",
            (_, _) => true,
            (context, _) => new MappersClass((INamedTypeSymbol)context.TargetSymbol)
        );

        GenerateReadMappers(initContext, mappersPipeline);

        GenerateWriteMappers(initContext, mappersPipeline);
    }
}
