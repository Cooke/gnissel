using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cooke.Gnissel.SourceGeneration;

[Generator]
public class ReaderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // initContext.RegisterPostInitializationOutput(ctx =>
        //     ctx.AddSource(
        //         "Test.cs",
        //         SourceText.From("namespace Test { public class Test { } }", Encoding.UTF8)
        //     )
        // );

        var dbContextSource = initContext.SyntaxProvider.CreateSyntaxProvider(
            (node, _) =>
                node is ClassDeclarationSyntax { BaseList.Types: var types }
                && types
                    .OfType<SimpleBaseTypeSyntax>()
                    .Select(x => x.Type)
                    .OfType<SimpleNameSyntax>()
                    .Any(type => type.Identifier.ValueText == "DbContext"),
            (context, ct) =>
                (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, ct)!
        );

        // initContext.RegisterSourceOutput(
        //     dbContextSource,
        //     (context, dbContextSymbol) =>
        //     {
        //         var stringWriter = new StringWriter();
        //         var writer = new IndentedTextWriter(stringWriter);
        //         writer.WriteLine("using System;");
        //         writer.WriteLine(
        //             $"namespace {dbContextSymbol.ContainingNamespace.ToDisplayString()};"
        //         );
        //         writer.WriteLine($"public partial class {dbContextSymbol.Name} {{");
        //         writer.Indent++;
        //         writer.Indent--;
        //         writer.WriteLine("}");
        //         writer.Flush();
        //         stringWriter.Flush();
        //
        //         context.AddSource(dbContextSymbol.Name, stringWriter.ToString());
        //     }
        // );

        var typesPipeline = initContext
            .SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                    node
                        is InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name: GenericNameSyntax
                                {
                                    Identifier.ValueText: "Query",
                                    TypeArgumentList.Arguments.Count: 1
                                }
                            }
                        },
                (context, ct) =>
                {
                    var invocation = (InvocationExpressionSyntax)context.Node;
                    var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                    var dbContextTypeInfo = context.SemanticModel.GetTypeInfo(
                        memberAccess.Expression,
                        ct
                    );

                    if (!IsDbContext(dbContextTypeInfo))
                    {
                        return null;
                    }

                    var genericName = (GenericNameSyntax)memberAccess.Name;
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    return context.SemanticModel.GetTypeInfo(typeArg).Type;
                }
            )
            .Where(type => type != null)
            .Select((input, _) => input!)
            .SelectMany(FindAllUsedTypes)
            .Collect()
            .SelectMany(
                (types, _) => types.Distinct(SymbolEqualityComparer.Default).Cast<ITypeSymbol>()
            );

        initContext.RegisterImplementationSourceOutput(
            typesPipeline,
            (context, type) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter);
                WriteObjectReaderField(sourceWriter, type);
                WriteReaderMetadata(sourceWriter, type);
                WriteReaderMethodStart(sourceWriter, type);
                WriteReaderBody(type, sourceWriter);
                WriteReaderMethodEnd(sourceWriter);
                WritePartialReaderClassEnd(sourceWriter);

                context.AddSource(
                    $"GeneratedObjectReaderProvider.{GetTypeIdentifierName(type)}.cs",
                    stringWriter.ToString()
                );
            }
        );
    }

    private IEnumerable<ITypeSymbol> FindAllUsedTypes(ITypeSymbol type, CancellationToken ct)
    {
        yield return type;

        var ctor = GetCtor(type);
        foreach (var t in ctor.Parameters)
        {
            if (!IsPrimitive(t.Type))
            {
                foreach (var innerType in FindAllUsedTypes(t.Type, ct))
                {
                    yield return innerType;
                }
            }
        }
    }

    private void WriteReaderMetadata(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        if (IsPrimitive(type))
        {
            sourceWriter.Write("private static readonly NextOrdinalReaderMetadata ");
            sourceWriter.Write(GetReaderMetadataName(type));
            sourceWriter.WriteLine(" = new ();");
        }
        else if (type.IsTupleType)
        {
            sourceWriter.Write("private static MultiReaderMetadata ");
            sourceWriter.Write(GetReaderMetadataName(type));
            sourceWriter.WriteLine(" => new ([");
            sourceWriter.Indent++;
            var ctor = GetCtor(type);
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                sourceWriter.Write(GetReaderMetadataName(ctor.Parameters[i].Type));

                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }

            sourceWriter.Indent--;
            sourceWriter.WriteLine("]);");
        }
        else
        {
            var ctor = GetCtor(type);

            sourceWriter.Write("private static MultiReaderMetadata ");
            sourceWriter.Write("Read");
            sourceWriter.Write(GetTypeIdentifierName(type));
            sourceWriter.Write("Metadata");
            sourceWriter.WriteLine(" => new ([");
            sourceWriter.Indent++;

            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                if (IsPrimitive(parameter.Type))
                {
                    sourceWriter.Write("new NameReaderMetadata(\"");
                    sourceWriter.Write(parameter.Name);
                    sourceWriter.Write("\")");
                }
                else
                {
                    sourceWriter.Write("new NestedReaderMetadata(\"");
                    sourceWriter.Write(parameter.Name);
                    sourceWriter.Write("\", ");
                    sourceWriter.Write(GetObjectReaderFieldName(parameter.Type));
                    sourceWriter.Write(")");
                }

                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }

            sourceWriter.Indent--;
            sourceWriter.WriteLine("]);");
        }

        sourceWriter.WriteLine();
    }

    private static string GetReaderMetadataName(ITypeSymbol type)
    {
        return $"Read{GetTypeIdentifierName(type)}Metadata";
    }

    private static string GetTypeIdentifierName(ITypeSymbol type)
    {
        return string.Join(
            "",
            type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat)
                .Select(x => x.Symbol?.Name)
                .Where(x => !string.IsNullOrEmpty(x))
        );
    }

    private static void WriteObjectReaderField(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("private readonly ObjectReader<");
        sourceWriter.Write(type.ToDisplayString());
        sourceWriter.Write("> ");
        sourceWriter.Write(GetObjectReaderFieldName(type));
        sourceWriter.WriteLine(";");
        sourceWriter.WriteLine();
    }

    private static string GetObjectReaderFieldName(ITypeSymbol type)
    {
        var typeDisplayName = GetTypeIdentifierName(type);
        return $"_{char.ToLower(typeDisplayName[0])}{typeDisplayName.Substring(1)}Reader";
    }

    private void WriteReaderBody(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (IsPrimitive(type))
        {
            sourceWriter.Write("return ");
            WriteRead(type, sourceWriter);

            if (!IsNullable(type))
            {
                sourceWriter.Write(" ?? ");
                sourceWriter.Write(
                    "throw new InvalidOperationException(\"Expected non-null value\")"
                );
            }

            sourceWriter.WriteLine(";");
        }
        else
        {
            var ctor = GetCtor(type);

            for (var i = 0; i < ctor!.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                sourceWriter.Write("var ");
                sourceWriter.Write(parameter.Name);
                sourceWriter.Write(" = ");
                WriteRead(parameter.Type, sourceWriter);
                sourceWriter.WriteLine(";");
            }
            sourceWriter.WriteLine();

            if (IsNullable(type) && ctor.Parameters.Length > 0)
            {
                sourceWriter.Write("if (");
                for (var i = 0; i < ctor.Parameters.Length; i++)
                {
                    var parameter = ctor.Parameters[i];
                    sourceWriter.Write(parameter.Name);
                    sourceWriter.Write(" is null");

                    if (i < ctor.Parameters.Length - 1)
                    {
                        sourceWriter.Write(" && ");
                    }
                }

                sourceWriter.WriteLine(")");
                sourceWriter.WriteLine("{");
                sourceWriter.Indent++;
                sourceWriter.WriteLine("return null;");
                sourceWriter.Indent--;
                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();
            }

            sourceWriter.Write("return ");
            if (!type.IsTupleType)
            {
                sourceWriter.Write("new ");
                sourceWriter.Write(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            sourceWriter.WriteLine("(");
            sourceWriter.Indent++;
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                sourceWriter.Write(parameter.Name);

                if (parameter.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    sourceWriter.Write(
                        " ?? throw new InvalidOperationException(\"Expected non-null value\")"
                    );
                }

                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
            sourceWriter.Indent--;
            sourceWriter.WriteLine(");");
        }
    }

    private static IMethodSymbol GetCtor(ITypeSymbol type) =>
        type.GetMembers(".ctor")
            .Cast<IMethodSymbol>()
            .OrderByDescending(x => x.Parameters.Length)
            .First();

    private void WriteRead(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        if (IsPrimitive(type))
        {
            WriteDbReaderNullableCall(type, sourceWriter);
        }
        else
        {
            sourceWriter.Write(GetObjectReaderFieldName(type));
            sourceWriter.Write(".Read(reader, ordinalReader)");
        }
    }

    private void WriteDbReaderCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        sourceWriter.Write("reader.Get");
        sourceWriter.Write(GetReaderGetSuffix(type));
        sourceWriter.Write("(ordinalReader.Read()");
        sourceWriter.Write(")");
    }

    private void WriteDbReaderNullableCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        sourceWriter.Write("reader.Get");
        sourceWriter.Write(GetReaderGetSuffix(type));
        sourceWriter.Write("OrNull(ordinalReader.Read()");
        sourceWriter.Write(")");
    }

    private static void WriteReaderMethodEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void WriteReaderMethodStart(IndentedTextWriter sourceWriter, ITypeSymbol type)
    {
        sourceWriter.Write("public ");
        sourceWriter.Write(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        sourceWriter.Write(" Read");
        sourceWriter.Write(type.Name);
        sourceWriter.WriteLine("(DbDataReader reader, OrdinalReader ordinalReader)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private static void WritePartialReaderClassStart(IndentedTextWriter sourceWriter)
    {
        sourceWriter.WriteLine("namespace Gnissel.SourceGeneration;");
        sourceWriter.WriteLine();
        sourceWriter.WriteLine("using System.Data.Common;");
        sourceWriter.WriteLine("using Cooke.Gnissel;");
        sourceWriter.WriteLine("using Cooke.Gnissel.SourceGeneration;");
        sourceWriter.WriteLine();
        sourceWriter.WriteLine("public partial class GeneratedObjectReaderProvider");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private static void WritePartialReaderClassEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
        sourceWriter.Flush();
    }

    private bool IsNullable(ITypeSymbol type) =>
        type is { Name: "Nullable" } || type.IsReferenceType;

    private bool IsPrimitive(ITypeSymbol readTypeType) =>
        readTypeType.Name switch
        {
            "Int32" or "String" => true,
            "Nullable" => true,
            _ => false,
        };

    private static bool IsDbContext(TypeInfo dbContextTypeInfo)
    {
        return dbContextTypeInfo.Type?.BaseType?.Name == "DbContext"
            || dbContextTypeInfo.Type?.Name == "DbContext";
    }

    private string GetReaderGetSuffix(ITypeSymbol type) =>
        type switch
        {
            { Name: "Nullable" } and INamedTypeSymbol namedTypeSymbol => namedTypeSymbol
                .TypeArguments[0]
                .Name,
            _ => type.Name,
        };
}
