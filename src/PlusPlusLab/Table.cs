using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;
using PlusPlusLab.Querying;
using PlusPlusLab.Utils;

namespace PlusPlusLab;

public class Table<T>(DbOptionsPlus options) : ITable, IToQuery<T>
{
    public string Name { get; } = options.IdentifierMapper.ToTableName(typeof(T));

    IReadOnlyCollection<IColumn> ITable.Columns => Columns;
    
    public IReadOnlyCollection<Column<T>> Columns { get; } = ColumnBuilder.CreateColumns<T>(options);

    public Type Type => typeof(T);
    
    public InsertQuery<T> Insert(T instance) 
        => new(this, Columns, options, [new(Columns.Select(c => c.CreateParameter(instance)).ToArray())]);
    
    public InsertQuery<T> Insert(params T[] instances) 
        => new(this, Columns, options, instances.Select(instance => new RowParameters(Columns.Select(c => c.CreateParameter(instance)).ToArray())).ToArray());
    
    public InsertQuery<T> Insert(IEnumerable<T> instances) 
        => new(this, Columns, options, instances.Select(instance => new RowParameters(Columns.Select(c => c.CreateParameter(instance)).ToArray())).ToArray());
    
    public DeleteQuery<T> Delete(Expression<Func<T, bool>> predicate) 
        => new DeleteQuery<T>(this, options, ParameterExpressionReplacer.Replace(predicate.Body, [
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

    public TypedQuery<TSelect> Select<TSelect>(Expression<Func<T, TSelect>> selector) 
        => new (options, CreateExpressionQuery().WithSelect(selector));

    public FirstOrDefaultQuery<T> FirstOrDefault(Expression<Func<T, bool>> predicate) 
        => new (options, CreateExpressionQuery().WithCondition(predicate));

    public TypedQuery<T> Where(Expression<Func<T, bool>> predicate) 
        => new (options, CreateExpressionQuery().WithCondition(predicate));

    public TypedQuery<T, TJoin> Join<TJoin>(Table<TJoin> joinTable, Expression<Func<T,TJoin, bool>> predicate) 
        => new (options, CreateExpressionQuery().WithJoin(joinTable, predicate));


    public FirstQuery<T> First() => new(options, CreateExpressionQuery());
    
    private ExpressionQuery CreateExpressionQuery() => new ExpressionQuery(new TableSource(this), null, [],  []);
    
    public Query<T> ToQuery() =>
        new Query<T>(
            options.DbAdapter.RenderSql(options.SqlGenerator.Generate(CreateExpressionQuery())),
            options.ObjectReaderProvider.GetReaderFunc<T>(),
            options.DbConnector
        );
}