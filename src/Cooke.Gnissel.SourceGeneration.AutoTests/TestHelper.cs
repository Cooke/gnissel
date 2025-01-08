using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Cooke.Gnissel.SourceGeneration.AutoTests;

public static class TestHelper
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }

    public static Task Verify(string source)
    {
        // Parse the provided string into a C# syntax tree
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable
            ),
            references: ReferenceAssemblies
                .Net.Net90.ResolveAsync((string?)null, CancellationToken.None)
                .Result.Add(MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location))
        );

        // Create an instance of our EnumGenerator incremental source generator
        var generator = new ReaderGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult();

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver).UseDirectory("snapshots");
    }
}
