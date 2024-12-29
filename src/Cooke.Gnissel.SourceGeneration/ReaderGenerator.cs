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

        var distinctQueries = queryPipline
            .Collect()
            .SelectMany((queries, ct) => queries.Distinct());

        initContext.RegisterSourceOutput(
            distinctQueries,
            (context, input) =>
            {
                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter);
                WriteReaderMethodStart(sourceWriter, input);

                var type = input.ReadType.Type!;
                var typeDisplayName = string.Join(
                    "",
                    type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat)
                        .Select(x => x.Symbol?.Name)
                        .Where(x => !string.IsNullOrEmpty(x))
                );

                sourceWriter.Write("return ");
                int readPosition = 0;
                WriteReaderCore(type, ref readPosition, null, sourceWriter);
                sourceWriter.WriteLine(";");

                WriteReaderMethodEnd(sourceWriter);
                WritePartialReaderClassEnd(sourceWriter);

                context.AddSource(
                    $"GeneratedObjectReaderProvider.{typeDisplayName}.cs",
                    stringWriter.ToString()
                );
            }
        );
    }

    private void WriteReaderCore(
        ITypeSymbol type,
        ref int readPosition,
        PathSegment? path,
        IndentedTextWriter sourceWriter
    )
    {
        if (IsPrimitive(type))
        {
            if (IsNullable(type))
            {
                WriteIsNullCall(readPosition, sourceWriter);
                sourceWriter.WriteLine("]) ? null : ");
            }

            WriteDbReaderCall(type, readPosition, sourceWriter);

            if (path is not null)
            {
                sourceWriter.Write(" /* ");
                sourceWriter.Write(path.ToMinimalDisplayString());
                sourceWriter.Write(" */");
            }

            readPosition++;
        }
        else
        {
            var ctor = type.GetMembers(".ctor")
                .Cast<IMethodSymbol>()
                .OrderByDescending(x => x.Parameters.Length)
                .FirstOrDefault();

            if (!type.IsTupleType)
            {
                sourceWriter.Write("new ");
                sourceWriter.Write(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            sourceWriter.WriteLine("(");
            sourceWriter.Indent++;
            for (var i = 0; i < ctor!.Parameters.Length; i++)
            {
                var parameter = ctor.Parameters[i];
                WriteReaderCore(
                    parameter.Type,
                    ref readPosition,
                    PathSegment.Combine(path, new ParameterPathSegment(parameter.Name)),
                    sourceWriter
                );
                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
            sourceWriter.Indent--;
            sourceWriter.Write(")");
        }
    }

    private static void WriteIsNullCall(int readPosition, IndentedTextWriter sourceWriter)
    {
        sourceWriter.WriteLine("reader.IsDBNull(columnOrdinals[");
        sourceWriter.WriteLine(readPosition);
        sourceWriter.WriteLine("])");
    }

    private void WriteDbReaderCall(
        ITypeSymbol type,
        int readPosition,
        IndentedTextWriter sourceWriter
    )
    {
        sourceWriter.Write("reader.Get");
        sourceWriter.Write(GetReaderGetSuffix(type));
        sourceWriter.Write("(columnOrdinals[");
        sourceWriter.Write(readPosition);
        sourceWriter.Write("])");
    }

    private static void WriteReaderMethodEnd(IndentedTextWriter sourceWriter)
    {
        sourceWriter.Indent--;
        sourceWriter.WriteLine("}");
    }

    private static void WriteReaderMethodStart(
        IndentedTextWriter sourceWriter,
        QueryInvocation input
    )
    {
        sourceWriter.Write("public ");
        sourceWriter.Write(
            input.ReadType.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
        sourceWriter.Write(" Read");
        sourceWriter.Write(input.ReadType.Type!.Name);
        sourceWriter.WriteLine("(DbDataReader reader, IReadOnlyList<int> columnOrdinals)");
        sourceWriter.WriteLine("{");
        sourceWriter.Indent++;
    }

    private static void WritePartialReaderClassStart(IndentedTextWriter sourceWriter)
    {
        sourceWriter.WriteLine("namespace Gnissel.SourceGeneration;");
        sourceWriter.WriteLine();
        sourceWriter.WriteLine("using System.Data.Common;");
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

    public abstract record PathSegment
    {
        public static PathSegment Combine(PathSegment? parent, PathSegment child) =>
            parent is null ? child : new NestedPathSegment(parent, child);

        public abstract string ToMinimalDisplayString();
    }

    public record ParameterPathSegment(string Name) : PathSegment
    {
        public string Name { get; } = Name;

        public override string ToMinimalDisplayString() => Name;
    }

    public record PropertyPathSegment(string Name) : PathSegment
    {
        public string Name { get; } = Name;

        public override string ToMinimalDisplayString() => Name;
    }

    public record NestedPathSegment(PathSegment Parent, PathSegment Child) : PathSegment
    {
        public PathSegment Parent { get; } = Parent;

        public PathSegment Child { get; } = Child;

        public override string ToMinimalDisplayString() => Child.ToMinimalDisplayString();
    }
}
