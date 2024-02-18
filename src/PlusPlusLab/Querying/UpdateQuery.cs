using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Queries;

namespace PlusPlusLab.Querying;

public interface IUpdateQuery
{
    ITable Table { get; }

    Expression? Condition { get; }

    IReadOnlyCollection<Setter> Setters { get; }
}

public record Setter(IColumn Column, Expression Value);

public interface ISetCollector<T>
{
    ISetCollector<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty value
    );

    ISetCollector<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    );
}


public class UpdateQuery<T>(
    Table<T> table,
    DbOptionsPlus options,
    Expression? predicate,
    IReadOnlyCollection<Setter> setters
) : IUpdateQuery
{
    public ITable Table { get; } = table;

    public Expression? Condition { get; } = predicate;

    public IReadOnlyCollection<Setter> Setters { get; } = setters;

    public ValueTaskAwaiter<int> GetAwaiter() => ExecuteAsync().GetAwaiter();

    public ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
        new NonQuery(
            options.DbConnector,
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(this))
        ).ExecuteAsync(cancellationToken);
}
