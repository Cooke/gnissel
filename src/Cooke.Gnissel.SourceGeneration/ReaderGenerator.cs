using System.CodeDom.Compiler;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

        var queryPipline = initContext
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
                                    TypeArgumentList: { Arguments: { Count: 1 } }
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
                    return new QueryInvocation(
                        new DbContextTypeInfo(
                            dbContextTypeInfo.Type!.ContainingNamespace.ToDisplayString(),
                            dbContextTypeInfo.Type.Name
                        ),
                        context.SemanticModel.GetTypeInfo(typeArg)
                    );
                }
            )
            .Where(input => input != null)
            .Select((input, _) => input!);

        initContext.RegisterSourceOutput(
            queryPipline,
            (context, input) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);

                var type = input.ReadType.Type!;

                if (IsPrimitive(type))
                {
                    if (IsNullable(type))
                    {
                        sourceWriter.Indent += 2;
                        sourceWriter.WriteLine($$"""if (reader.IsDBNull(columnOrdinals[0])) {""");
                        sourceWriter.Indent++;
                        sourceWriter.WriteLine("return default;");
                        sourceWriter.Indent--;
                        sourceWriter.WriteLine("}");
                    }

                    sourceWriter.WriteLine(
                        $"return reader.Get{GetReaderGetSuffix(type)}(columnOrdinals[0]);"
                    );
                }

                var nullablePrefix = IsNullable(type) ? "Nullable" : "";
                var typeDisplayName =
                    nullablePrefix
                    + string.Join(
                        "",
                        type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat)
                            .Select(x => x.Symbol?.Name)
                            .Where(x => !string.IsNullOrEmpty(x))
                    );

                var sourceText = SourceText.From(
                    // csharpier-ignore-start
                    $$"""
                    namespace Gnissel.SourceGeneration;

                    using System.Data.Common;

                    public partial class GeneratedObjectReaderProvider
                    {
                        public {{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} Read{{typeDisplayName}}(DbDataReader reader, IReadOnlyList<int> columnOrdinals)
                        {
                            {{stringWriter.ToString()}}
                        }
                    }
                    """,
                    // csharpier-ignore-end
                    Encoding.UTF8
                );

                context.AddSource(
                    $"GeneratedObjectReaderProvider.{typeDisplayName}.cs",
                    sourceText
                );
            }
        );
    }

    private bool IsNullable(ITypeSymbol type) => type is { Name: "Nullable" };

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

    private class QueryInvocation(DbContextTypeInfo dbContextType, TypeInfo readType)
    {
        public DbContextTypeInfo DbContextType { get; } = dbContextType;

        public TypeInfo ReadType { get; } = readType;

        protected bool Equals(QueryInvocation other)
        {
            return DbContextType.Equals(other.DbContextType) && ReadType.Equals(other.ReadType);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((QueryInvocation)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (DbContextType.GetHashCode() * 397) ^ ReadType.GetHashCode();
            }
        }
    }

    private class DbContextTypeInfo(string @namespace, string name)
    {
        public string Namespace { get; } = @namespace;
        public string Name { get; } = name;

        protected bool Equals(DbContextTypeInfo other)
        {
            return Namespace == other.Namespace && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((DbContextTypeInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Namespace.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
    }
}
