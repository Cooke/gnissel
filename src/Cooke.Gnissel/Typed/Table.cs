using System.Collections.Immutable;
using System.Data.Common;
using System.Linq.Expressions;
using Cooke.Gnissel.Internals;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;
using Cooke.Gnissel.Typed.Queries;

namespace Cooke.Gnissel.Typed;

public interface ITable
{
    string Name { get; }

    IReadOnlyCollection<IColumn> Columns { get; }

    Type Type { get; }
}

public record TableOptions(DbOptions DbOptions)
{
    public string? Name { get; init; }
};

public class Table<T> : ITable, IQuery<T>
{
    private readonly DbOptions _options;
    private readonly Query<T> _query;
    private readonly ObjectWriter<T> _writer;

    public Table(Table<T> copy, DbOptions options)
        : this(options)
    {
        Columns = copy.Columns;
        Name = copy.Name;
        _options = options;
    }

    public Table(DbOptions options)
        : this(new TableOptions(options)) { }

    public Table(TableOptions options)
    {
        var dbOptions = options.DbOptions;
        _writer = dbOptions.GetWriter<T>();
        Columns = ColumnBuilder.CreateColumns<T>(options).ToImmutableArray();
        Name =
            options.Name
            ?? options.DbOptions.MapperProvider.NameProvider.ToTableName(typeof(T).Name)
            ?? throw new InvalidOperationException("Table name is not set and cannot be inferred");
        _options = dbOptions;
        var objectReader = dbOptions.GetReader<T>();

        _query = new Query<T>(
            dbOptions.RenderSql(dbOptions.DbAdapter.Generate(CreateExpressionQuery(), dbOptions)),
            (reader, cancellationToken) => reader.ReadRows(objectReader, cancellationToken),
            dbOptions.DbConnector
        );
    }

    public string Name { get; }

    IReadOnlyCollection<IColumn> ITable.Columns => Columns;

    public IReadOnlyCollection<Column<T>> Columns { get; }

    public Type Type => typeof(T);

    public InsertQuery<T> Insert(T instance) =>
        new(this, Columns, _options, [CreateRowParameters(instance)]);

    public InsertQuery<T> Insert(params T[] instances) =>
        new(this, Columns, _options, instances.Select(CreateRowParameters).ToArray());

    public InsertQuery<T> Insert(IEnumerable<T> instances) =>
        new(this, Columns, _options, instances.Select(CreateRowParameters).ToArray());

    public DeleteQueryWithoutWhere<T> Delete() => new(this, _options);

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty value
    ) => new(this, _options, [SetterFactory.CreateSetter(this, propertySelector, value)]);

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    ) => new(this, _options, [SetterFactory.CreateSetter(this, propertySelector, value)]);

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) =>
        new(CreateExpressionQuery().Select(selector));

    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(CreateExpressionQuery().Where(predicate));

    public SingleQuery<T> First() => CreateExpressionQuery().First<T>();

    public SingleQuery<T> First(Expression<Func<T, bool>> predicate) =>
        CreateExpressionQuery().First<T>(predicate);

    public SingleOrDefaultQuery<T> FirstOrDefault() => CreateExpressionQuery().FirstOrDefault<T>();

    public SingleOrDefaultQuery<T> FirstOrDefault(Expression<Func<T, bool>> predicate) =>
        CreateExpressionQuery().FirstOrDefault<T>(predicate);

    public OrderByQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(CreateExpressionQuery().OrderBy(propSelector));

    public OrderByQuery<T> OrderByDesc<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(CreateExpressionQuery().OrderByDesc(propSelector));

    public GroupByQuery<T> GroupBy<TProp>(Expression<Func<T, TProp>> propSelector) =>
        new(CreateExpressionQuery().GroupBy(propSelector));

    public TypedQuery<T, TJoin> Join<TJoin>(
        Table<TJoin> joinTable,
        Expression<Func<T, TJoin, bool>> predicate
    ) => new(CreateExpressionQuery().Join(joinTable, predicate));

    public TypedQuery<T, T2?> LeftJoin<T2>(
        Table<T2> joinTable,
        Expression<Func<T, T2, bool>> predicate
    ) => new(CreateExpressionQuery().LeftJoin(joinTable, predicate));

    public TypedQuery<T?, T2> RightJoin<T2>(
        Table<T2> joinTable,
        Expression<Func<T, T2, bool>> predicate
    ) => new(CreateExpressionQuery().RightJoin(joinTable, predicate));

    public TypedQuery<T?, T2?> FullJoin<T2>(
        Table<T2> joinTable,
        Expression<Func<T, T2, bool>> predicate
    ) => new(CreateExpressionQuery().FullJoin(joinTable, predicate));

    public TypedQuery<T, T2> CrossJoin<T2>(Table<T2> joinTable) =>
        new(CreateExpressionQuery().CrossJoin(joinTable));

    public TypedQuery<T> Limit(int limit) => new(CreateExpressionQuery() with { Limit = limit });

    private ExpressionQuery CreateExpressionQuery() =>
        new(_options, new TableSource(this), null, [], [], [], []);

    private RowParameters CreateRowParameters(T instance)
    {
        var parameterWriter = new ListParameterWriter(_options, Columns.Count);
        _writer.Write(instance, parameterWriter);
        return new(parameterWriter.Parameters);
    }

    private class ListParameterWriter(DbOptions options, int numParameters) : IParameterWriter
    {
        public List<DbParameter> Parameters { get; } = new(numParameters);

        public void Write<T>(T value, string? dbType = null) =>
            Parameters.Add(options.CreateParameter(value, dbType));
    }

    public RenderedSql RenderedSql => _query.RenderedSql;

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default) =>
        _query.ExecuteAsync(cancellationToken);
}
