using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cooke.Gnissel;

public interface IObjectMapper
{
    TOut Map<TOut>(Row row);
}

public class ObjectMapper : IObjectMapper
{
    private readonly ConcurrentDictionary<Type, object> _mappers =
        new ConcurrentDictionary<Type, object>();

    public TOut Map<TOut>(Row row)
    {
        var mapper = (Func<Row, TOut>)_mappers.GetOrAdd(typeof(TOut), () => CreateTypeMapper<TOut>);
        return mapper(row);
    }

    private static Func<Row, TOut> CreateTypeMapper<TOut>()
    {
        var ctor = typeof(TOut).GetConstructors().First();
        var row = Expression.Parameter(typeof(Row));
        var mapper = Expression
            .Lambda<Func<Row, TOut>>(
                Expression.New(
                    ctor,
                    ctor.GetParameters()
                        .Select(
                            (p, i) =>
                                Expression.Call(
                                    row,
                                    "Get",
                                    new[] { p.ParameterType },
                                    Expression.Constant(i)
                                )
                        )
                ),
                row
            )
            .Compile();
        return mapper;
    }
}
