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

public class Table<T> : IAsyncEnumerable<T>
{
    private readonly IDbConnector _dbConnector;
    private readonly string _insertCommandText;
    private readonly WhereQuery<T> _whereQuery;

    public Table(DbOptions options)
    {
        var dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        Columns = CreateColumns(options);
        _whereQuery = new WhereQuery<T>(options, Name, null, Columns);
        _insertCommandText = dbAdapter.RenderSql(CreateInsertSql(dbAdapter)).CommandText;
    }

    public ImmutableArray<Column<T>> Columns { get; set; }

    public Table(Table<T> source, DbOptions options)
    {
        var dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        _insertCommandText = source._insertCommandText;
        Columns = source.Columns;
    }

    public string Name { get; } = typeof(T).Name.ToLower() + "s";

    private static ImmutableArray<Column<T>> CreateColumns(DbOptions dbOptions)
    {
        var objectParameter = Expression.Parameter(typeof(T));
        return typeof(T)
            .GetProperties()
            .SelectMany(
                p =>
                    CreateColumns(
                        dbOptions,
                        p,
                        objectParameter,
                        Expression.Property(objectParameter, p)
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns(
        DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression
    )
    {
        if (p.GetDbType() != null)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression);
        else if (p.PropertyType.IsClass)
            foreach (
                var column in p.PropertyType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(
                        innerProperty =>
                            CreateColumns(
                                dbOptions,
                                innerProperty,
                                rootExpression,
                                Expression.Property(memberExpression, innerProperty)
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression);
    }

    private static Column<T> CreateColumn(
        DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression
    )
    {
        return new Column<T>(
            p.GetDbName() ?? dbOptions.DbAdapter.DefaultIdentifierMapper.ToColumnName(p),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                ?.Let(x => x.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity) ?? false,
            p,
            CreateParameterFactory(
                dbOptions.DbAdapter,
                memberExpression,
                p.GetDbType(),
                objectExpression
            )
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
    public ExecuteQuery Insert(T item)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity);

        var sql = new RenderedSql(
            _insertCommandText,
            insertColumns.Select(col => col.CreateParameter(item)).ToArray()
        );
        return new ExecuteQuery(_dbConnector, sql, CancellationToken.None);
    }

    [Pure]
    public ExecuteQuery Insert(params T[] items)
    {
        return Insert((IEnumerable<T>)items);
    }

    [Pure]
    public ExecuteQuery Insert(IEnumerable<T> items)
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
                if (j > 0)
                    sb.Append(", ");

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
        return new ExecuteQuery(_dbConnector, sql, CancellationToken.None);
    }

    private Sql CreateInsertSql(IDbAdapter dbAdapter)
    {
        var sql = new Sql(20 + Columns.Length * 4);
        sql.AppendLiteral("INSERT INTO ");
        sql.AppendLiteral(dbAdapter.EscapeIdentifier(Name));
        sql.AppendLiteral(" (");
        var firstColumn = true;
        foreach (var column in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstColumn)
                sql.AppendLiteral(", ");
            sql.AppendLiteral(dbAdapter.EscapeIdentifier(column.Name));
            firstColumn = false;
        }

        sql.AppendLiteral(") VALUES(");
        var firstParam = true;
        foreach (var dbParameter in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstParam)
                sql.AppendLiteral(", ");
            sql.AppendFormatted(dbParameter);
            firstParam = false;
        }

        sql.AppendLiteral(")");
        return sql;
    }

    [Pure]
    public Query<TOut> Select<TOut>(Expression<Func<T, TOut>> selector) =>
        _whereQuery.Select(selector);

    [Pure]
    public WhereQuery<T> Where(Expression<Func<T, bool>> predicate) => _whereQuery.Where(predicate);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        _whereQuery.GetAsyncEnumerator(cancellationToken);
}
