using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cooke.Gnissel;

public class ObjectMapper : IObjectMapper
{
    private readonly ConcurrentDictionary<Type, object> _mappers =
        new ConcurrentDictionary<Type, object>();

    public TOut Map<TOut>(RowReader rowReader)
    {
        var mapper = (Func<RowReader, TOut>)_mappers.GetOrAdd(typeof(TOut), _ => CreateTypeMapper<TOut>());
        return mapper(rowReader);
    }

    private static Func<RowReader, TOut> CreateTypeMapper<TOut>()
    {
        var ctor = typeof(TOut).GetConstructors().First();
        var row = Expression.Parameter(typeof(RowReader));
        var mapper = Expression
            .Lambda<Func<RowReader, TOut>>(
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
