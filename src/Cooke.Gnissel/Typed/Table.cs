using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;
using Cooke.Gnissel.Typed.Queries;
using Cooke.Gnissel.Utils;

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

    public IReadOnlyCollection<ColumnOptions> Columns { get; init; } = [];

    public IReadOnlyCollection<IReadOnlyCollection<MemberInfo>> Ignores { get; init; } = [];
};

public record ColumnOptions(IReadOnlyCollection<MemberInfo> MemberChain)
{
    public string? Name { get; init; }

    public bool IsDatabaseGenerated { get; init; }
}

public class TableOptionsBuilder<T>(DbOptions dbOptions)
{
    private string? name;
    private readonly List<ColumnOptions> _columns = new();
    private readonly List<IReadOnlyCollection<MemberInfo>> _ignores = new();

    public TableOptionsBuilder<T> Name(string name)
    {
        this.name = name;
        return this;
    }

    public TableOptions Build() =>
        new(dbOptions)
        {
            Name = name,
            Columns = _columns.ToArray(),
            Ignores = _ignores,
        };

    public TableOptionsBuilder<T> Column<TProp>(
        Expression<Func<T, TProp>> selector,
        Action<ColumnOptionsBuilder> configure
    )
    {
        var memberChain = ExpressionUtils.GetMemberChain(selector.Body);
        var columnOptionsBuilder = new ColumnOptionsBuilder(memberChain);
        configure(columnOptionsBuilder);
        _columns.Add(columnOptionsBuilder.Build());
        return this;
    }

    public TableOptionsBuilder<T> Column<TProp>(
        Expression<Func<T, TProp>> selector,
        string columnName
    )
    {
        return Column(selector, x => x.Name(columnName));
    }

    public TableOptionsBuilder<T> Ignore<TProp>(Expression<Func<T, TProp>> selector)
    {
        _ignores.Add(ExpressionUtils.GetMemberChain(selector.Body));
        return this;
    }
}

public class ColumnOptionsBuilder(IReadOnlyCollection<MemberInfo> memberChain)
{
    private string? _name;

    public ColumnOptionsBuilder Name(string name)
    {
        this._name = name;
        return this;
    }

    public ColumnOptions Build() => new ColumnOptions(memberChain) { Name = _name };
}

public class Table<T> : ITable, IQuery<T>
{
    private readonly DbOptions options;
    private readonly Column<T>[] insertColumns;
    private readonly Query<T> _query;

    public Table(Table<T> copy, DbOptions options)
        : this(options)
    {
        Columns = copy.Columns;
        Name = copy.Name;
        this.options = options;
        insertColumns = copy.insertColumns;
    }

    public Table(DbOptions options)
        : this(new TableOptions(options)) { }

    public Table(TableOptions options)
    {
        var dbOptions = options.DbOptions;
        Columns = ColumnBuilder.CreateColumns<T>(options);
        Name = options.Name ?? dbOptions.DbAdapter.ToTableName(typeof(T));
        this.options = dbOptions;
        insertColumns = Columns.Where(x => !x.IsDatabaseGenerated).ToArray();
        _query = new Query<T>(
            dbOptions.RenderSql(
                dbOptions.TypedSqlGenerator.Generate(CreateExpressionQuery(), dbOptions)
            ),
            dbOptions.ObjectReaderProvider.GetReaderFunc<T>(dbOptions),
            dbOptions.DbConnector
        );
    }

    public string Name { get; }

    IReadOnlyCollection<IColumn> ITable.Columns => Columns;

    public IReadOnlyCollection<Column<T>> Columns { get; }

    public Type Type => typeof(T);

    public InsertQuery<T> Insert(T instance) =>
        new(this, insertColumns, options, [CreateRowParameters(instance)]);

    public InsertQuery<T> Insert(params T[] instances) =>
        new(this, insertColumns, options, instances.Select(CreateRowParameters).ToArray());

    public InsertQuery<T> Insert(IEnumerable<T> instances) =>
        new(this, insertColumns, options, instances.Select(CreateRowParameters).ToArray());

    public DeleteQueryWithoutWhere<T> Delete() => new(this, options);

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty value
    ) => new(this, options, [SetterFactory.CreateSetter(this, propertySelector, value)]);

    public UpdateQueryWithoutWhere<T> Set<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        Expression<Func<T, TProperty>> value
    ) => new(this, options, [SetterFactory.CreateSetter(this, propertySelector, value)]);

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
        new(options, new TableSource(this), null, [], [], [], []);

    private RowParameters CreateRowParameters(T instance) =>
        new(
            Columns
                .Where(x => !x.IsDatabaseGenerated)
                .Select(c => c.CreateParameter(instance))
                .ToArray()
        );

    public RenderedSql RenderedSql => _query.RenderedSql;

    public IAsyncEnumerable<T> ExecuteAsync(CancellationToken cancellationToken = default) =>
        _query.ExecuteAsync(cancellationToken);
}
