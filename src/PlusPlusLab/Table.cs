using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel;
using Cooke.Gnissel.PlusPlus.Utils;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

namespace PlusPlusLab;

public class Table<T> : ITable, IToAsyncEnumerable<T>
{
    private readonly DbOptionsPlus options;
    private readonly ExpressionQuery _expressionQuery;

    public Table(DbOptionsPlus options)
    {
        this.options = options;
        Columns = CreateColumns(options, this);
        Name = options.IdentifierMapper.ToTableName(typeof(T));
        _expressionQuery = new ExpressionQuery(new TableSource(this), null, [],  []);
    }

    public string Name { get; }

    IReadOnlyCollection<IColumn> ITable.Columns => Columns;
    
    public IReadOnlyCollection<Column<T>> Columns { get; }
    
    public Type Type => typeof(T);
    
    public InsertQuery<T> Insert(T instance) => new InsertQuery<T>(this, Columns, options, Columns.Select(c => c.CreateParameter(instance)).ToArray());
    
    public DeleteQuery<T> Delete(Expression<Func<T, bool>> predicate) => new DeleteQuery<T>(this, options, ParameterExpressionReplacer.Replace(predicate.Body, [
        (predicate.Parameters.Single(), new TableExpression(new TableSource(this)))
    ]));

    public TypedQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector)
    {
        var tableSource = new TableSource(this);
        var expressionQuery = new ExpressionQuery(
            tableSource, 
            ParameterExpressionReplacer.Replace(selector.Body, [
                (selector.Parameters.Single(), new TableExpression(tableSource))
            ])
            , [], []);
        return new TypedQuery<TSelect>(options, expressionQuery);
    }
    
    public FirstOrDefaultQuery<T> FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        var tableSource = new TableSource(this);
        var expressionQuery = new ExpressionQuery(
            tableSource, 
            null
            , [],
            [ParameterExpressionReplacer.Replace(predicate.Body, [
                (predicate.Parameters.Single(), new TableExpression(tableSource))
            ])],
            Limit: 1);
        return new(options, expressionQuery);
    }

    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        var tableSource = new TableSource(this);
        var expressionQuery = new ExpressionQuery(
            tableSource, 
            null
            , [],
            [ParameterExpressionReplacer.Replace(predicate.Body, [
                (predicate.Parameters.Single(), new TableExpression(tableSource))
            ])]);
        return new(options, expressionQuery);
    }

    private static ImmutableArray<Column<T>> CreateColumns(DbOptions dbOptions, Table<T> table)
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
                        Expression.Property(objectParameter, p),
                        table
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression, Table<T> table)
    {
        if (p.GetDbType() != null)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
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
                                Expression.Property(memberExpression, innerProperty), table
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
    }

    private static Column<T> CreateColumn(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression, Table<T> table)
    {
        return new Column<T>(
            table,
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
                    [valueExpression.Type],
                    valueExpression,
                    Expression.Constant(dbType, typeof(string))
                ),
                tableItemParameter
            )
            .Compile();
    }

    public IAsyncEnumerable<T> ToAsyncEnumerable()
    {
        return new Query<T>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(_expressionQuery)),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );
    }
    
}