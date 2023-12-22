using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Statements;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel;

public class Table<T> : TableQueryStatement<T>
{
    private readonly IDbAdapter _dbAdapter;
    private readonly IDbConnector _dbConnector;
    private readonly string _insertCommandText;

    public Table(DbOptions options)
        : base(options, typeof(T).Name.ToLower() + "s", null, CreateColumns(options.DbAdapter))
    {
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;

        var sql = new Sql(20 + Columns.Length * 4);
        sql.AppendLiteral("INSERT INTO ");
        sql.AppendLiteral(_dbAdapter.EscapeIdentifier(Name));
        sql.AppendLiteral(" (");
        var firstColumn = true;
        foreach (var column in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstColumn) sql.AppendLiteral(", ");
            sql.AppendLiteral(_dbAdapter.EscapeIdentifier(column.Name));
            firstColumn = false;
        }

        sql.AppendLiteral(") VALUES(");
        var firstParam = true;
        foreach (var dbParameter in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstParam) sql.AppendLiteral(", ");
            sql.AppendFormatted(dbParameter);
            firstParam = false;
        }

        sql.AppendLiteral(")");
        _insertCommandText = _dbAdapter.RenderSql(sql).CommandText;
    }

    public Table(Table<T> source, DbOptions options)
        : base(options, source.Name, null, source.Columns)
    {
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        _insertCommandText = source._insertCommandText;
    }

    public string Name { get; } = typeof(T).Name.ToLower() + "s";

    private static ImmutableArray<Column<T>> CreateColumns(IDbAdapter dbAdapter)
    {
        var objectParameter = Expression.Parameter(typeof(T));
        return typeof(T)
            .GetProperties()
            .SelectMany(
                p =>
                    CreateColumns(
                        dbAdapter,
                        p,
                        objectParameter,
                        Expression.Property(objectParameter, p)
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns(
        IDbAdapter dbAdapter,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression
    )
    {
        if (p.GetDbType() != null)
            yield return CreateColumn(dbAdapter, p, rootExpression, memberExpression);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn(dbAdapter, p, rootExpression, memberExpression);
        else if (p.PropertyType.IsClass)
            foreach (
                var column in p.PropertyType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(
                        innerProperty =>
                            CreateColumns(
                                dbAdapter,
                                innerProperty,
                                rootExpression,
                                Expression.Property(memberExpression, innerProperty)
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn(dbAdapter, p, rootExpression, memberExpression);
    }

    private static Column<T> CreateColumn(
        IDbAdapter dbAdapter,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression
    )
    {
        return new Column<T>(
            p.GetDbName() ?? dbAdapter.DefaultIdentifierMapper.ToColumnName(p),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                ?.Let(x => x.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity) ?? false,
            p,
            CreateParameterFactory(dbAdapter, memberExpression, p.GetDbType(), objectExpression)
        );
    }

    private static Func<T, DbParameter> CreateParameterFactory(
        IDbAdapter dbAdapter,
        Expression valueExpression,
        string? dbType,
        ParameterExpression tableItemParameter
    )
    {
        return Expression
            .Lambda<Func<T, DbParameter>>(
                Expression.Call(
                    Expression.Constant(dbAdapter),
                    nameof(dbAdapter.CreateParameter),
                    new[] { valueExpression.Type },
                    valueExpression,
                    Expression.Constant(dbType, typeof(string))
                ),
                tableItemParameter
            )
            .Compile();
    }

    [Pure]
    public ExecuteStatement Insert(T item)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity);

        var sql = new RenderedSql(
            _insertCommandText,
            insertColumns.Select(col => col.CreateParameter(item)).ToArray()
        );
        return new ExecuteStatement(_dbConnector, sql, CancellationToken.None);
    }

    [Pure]
    public ExecuteStatement Insert(params T[] items)
    {
        return Insert((IEnumerable<T>)items);
    }

    [Pure]
    public ExecuteStatement Insert(IEnumerable<T> items)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity).ToArray();
        var itemsArray = items as ICollection<T> ?? items.ToArray();
        var sb = new StringBuilder(
            _insertCommandText.Length + insertColumns.Length * 6 * itemsArray.Count
        );
        sb.Append(_insertCommandText);
        for (var i = 1; i < itemsArray.Count; i++)
        {
            sb.Append(", (");
            for (var j = 0; j < insertColumns.Length; j++)
            {
                if (j > 0) sb.Append(", ");

                sb.Append('$');
                sb.Append(i * insertColumns.Length + j + 1);
            }

            sb.Append(") ");
        }

        var sql = new RenderedSql(
            sb.ToString(),
            itemsArray
                .SelectMany(item => insertColumns.Select(col => col.CreateParameter(item)))
                .ToArray()
        );
        return new ExecuteStatement(_dbConnector, sql, CancellationToken.None);
    }
}