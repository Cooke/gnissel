using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;

namespace Cooke.Gnissel.Typed.Queries;

public interface IUpdateQuery : INonQuery
{
    ITable Table { get; }

    Expression? Condition { get; }

    IReadOnlyCollection<Setter> Setters { get; }
}

public abstract record Setter(IColumn Column);

public record ExpressionSetter(IColumn Column, Expression Value) : Setter(Column);

public abstract record ValueSetter(IColumn Column) : Setter(Column)
{
    public abstract void AppendParameter(Sql sql);
}

public record ValueSetter<T>(IColumn Column, T Value) : ValueSetter(Column)
{
    public override void AppendParameter(Sql sql)
    {
        sql.AppendParameter(Value);
    }
}

public class UpdateQueryWithoutWhere<T>(
    Table<T> table,
    DbOptions options,
    IReadOnlyCollection<Setter> setters
)
{
    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty value
    ) =>
        new(
            table,
            options,
            [.. setters, SetterFactory.CreateSetter(table, propertySelector, value)]
        );

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    ) =>
        new(
            table,
            options,
            [.. setters, SetterFactory.CreateSetter(table, propertySelector, value)]
        );

    public UpdateQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(
            table,
            options,
            ParameterExpressionReplacer.Replace(
                predicate.Body,
                [(predicate.Parameters.Single(), new TableExpression(new TableSource(table)))]
            ),
            setters
        );

    public UpdateQuery<T> WithoutWhere() => new(table, options, null, setters);
}

public class UpdateQuery<T>(
    Table<T> table,
    DbOptions options,
    Expression? predicate,
    IReadOnlyCollection<Setter> setters
) : IUpdateQuery
{
    public ITable Table { get; } = table;

    public Expression? Condition { get; } = predicate;

    public IReadOnlyCollection<Setter> Setters { get; } = setters;

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty value
    ) =>
        new(
            table,
            options,
            [.. Setters, SetterFactory.CreateSetter(table, propertySelector, value)]
        );

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    ) =>
        new(
            table,
            options,
            [.. Setters, SetterFactory.CreateSetter(table, propertySelector, value)]
        );

    public ValueTaskAwaiter<int> GetAwaiter() => ExecuteAsync().GetAwaiter();

    public UpdateQueryWithoutWhere<T> Where(Expression<Func<T, bool>> predicate) =>
        new(table, options, Setters);

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
        new NonQuery(options.DbConnector, RenderedSql).ExecuteAsync(cancellationToken);

    public RenderedSql RenderedSql =>
        options.RenderSql(options.TypedSqlGenerator.Generate(this, options));
}
