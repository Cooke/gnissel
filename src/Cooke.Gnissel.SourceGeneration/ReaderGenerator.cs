using System.CodeDom.Compiler;
using System.Collections.Immutable;
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

        initContext.RegisterImplementationSourceOutput(
            distinctQueries,
            (context, input) =>
            {
                var type = input.ReadType.Type!;
                var typeDisplayName = GetTypeDisplayName(type);

                var stringWriter = new StringWriter();
                var sourceWriter = new IndentedTextWriter(stringWriter);
                WritePartialReaderClassStart(sourceWriter);
                WriteObjectReaderField(sourceWriter, type);
                WriteReaderMethodStart(sourceWriter, input);

                sourceWriter.Write("return ");
                WriteReaderCore(type, ImmutableArray<string>.Empty, sourceWriter);
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

    private static string GetTypeDisplayName(ITypeSymbol type)
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
        var typeDisplayName = GetTypeDisplayName(type);
        return $"_{char.ToLower(typeDisplayName[0])}{typeDisplayName.Substring(1)}Reader";
    }

    private void WriteReaderCore(
        ITypeSymbol type,
        ImmutableArray<string> path,
        IndentedTextWriter sourceWriter
    )
    {
        if (IsPrimitive(type))
        {
            if (IsNullable(type))
            {
                WriteIsNullCall(sourceWriter, GetObjectReaderFieldName(type));
                sourceWriter.WriteLine("]) ? null : ");
            }

            WriteDbReaderCall(type, sourceWriter);
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
                WriteReaderCore(parameter.Type, path.Add(parameter.Name), sourceWriter);
                if (i < ctor.Parameters.Length - 1)
                {
                    sourceWriter.WriteLine(",");
                }
            }
            sourceWriter.Indent--;
            sourceWriter.Write(")");
        }
    }

    private static void WriteIsNullCall(IndentedTextWriter sourceWriter, string readerName)
    {
        sourceWriter.WriteLine("ObjectReaderUtils.IsNull(reader, ordinalReader, ");
        sourceWriter.WriteLine(readerName);
        sourceWriter.WriteLine(")");
    }

    private void WriteDbReaderCall(ITypeSymbol type, IndentedTextWriter sourceWriter)
    {
        sourceWriter.Write("reader.Get");
        sourceWriter.Write(GetReaderGetSuffix(type));
        sourceWriter.Write("(ordinalReader.Read()");
        sourceWriter.Write(")");
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
}
