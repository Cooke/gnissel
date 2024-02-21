using System.Linq.Expressions;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Typed.Internals;
using Cooke.Gnissel.Typed.Querying;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.Typed;

public interface ITable
{
    string Name { get; }

    IReadOnlyCollection<IColumn> Columns { get; }

    Type Type { get; }
}


public class Table<T> : ITable, IToQuery<T>
{
    private readonly DbOptionsTyped options;
    private readonly Column<T>[] insertColumns;

    public Table(Table<T> copy, DbOptionsTyped options) : this(options)
    {
        Columns = copy.Columns;
        Name = copy.Name;
        this.options = options;
        insertColumns = copy.insertColumns;
    }

    public Table(DbOptionsTyped options)
    {
        Columns = ColumnBuilder.CreateColumns<T>(options);
        Name = options.IdentifierMapper.ToTableName(typeof(T));
        this.options = options;
        insertColumns = Columns.Where(x => !x.IsDatabaseGenerated).ToArray();
    }
    
    public string Name { get; }

    IReadOnlyCollection<IColumn> ITable.Columns => Columns;
    
    public IReadOnlyCollection<Column<T>> Columns { get; } 

    public Type Type => typeof(T);
    
    public InsertQuery<T> Insert(T instance) 
        => new (this, insertColumns, options, [CreateRowParameters(instance)]);

    public InsertQuery<T> Insert(params T[] instances) 
        => new(this, insertColumns, options, instances.Select(CreateRowParameters).ToArray());
    
    public InsertQuery<T> Insert(IEnumerable<T> instances) 
        => new(this, insertColumns, options, instances.Select(CreateRowParameters).ToArray());
    
    public DeleteQuery<T> Delete(Expression<Func<T, bool>> predicate) 
        => new (this, options, ParameterExpressionReplacer.Replace(predicate.Body, [
        (predicate.Parameters.Single(), new TableExpression(new TableSource(this)))
    ]));
    
    public UpdateQuery<T> Update(Expression<Func<T, bool>> predicate, Func<ISetCollector<T>, ISetCollector<T>> collectSetters)
    {
        var collector = new SetCollector<T>(this);
        collectSetters(collector);
        
        return new UpdateQuery<T>(
            this, options, ParameterExpressionReplacer.Replace(predicate.Body, [
                (predicate.Parameters.Single(), new TableExpression(new TableSource(this)))
            ]),
            collector.Setters);
    }

    public SelectQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) 
        => new(CreateExpressionQuery().Select(selector));

    public FirstOrDefaultQuery<T> FirstOrDefault(Expression<Func<T, bool>> predicate) 
        => new (CreateExpressionQuery().Where(predicate));

    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) 
        => new (CreateExpressionQuery().Where(predicate));

    public TypedQuery<T, TJoin> Join<TJoin>(Table<TJoin> joinTable, Expression<Func<T,TJoin, bool>> predicate) 
        => new (CreateExpressionQuery().Join(joinTable, predicate));


    public FirstQuery<T> First() => new(CreateExpressionQuery());
    
    private ExpressionQuery CreateExpressionQuery() 
        => new (options, new TableSource(this), null, [],  [], [], []);
    
    public Query<T> ToQuery() =>
        new Query<T>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(CreateExpressionQuery())),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );
    
    private RowParameters CreateRowParameters(T instance) 
        => new RowParameters(Columns.Where(x => !x.IsDatabaseGenerated).Select(c => c.CreateParameter(instance)).ToArray());

    public OrderByQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> propSelector) 
        => new(CreateExpressionQuery().OrderBy(propSelector));

    public OrderByQuery<T> OrderByDesc<TProp>(Expression<Func<T, TProp>> propSelector) 
        => new(CreateExpressionQuery().OrderByDesc(propSelector));

    public GroupByQuery<T> GroupBy<TProp>(Expression<Func<T, TProp>> propSelector) 
        => new(CreateExpressionQuery().GroupBy(propSelector));
}