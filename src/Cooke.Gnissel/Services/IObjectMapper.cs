namespace Cooke.Gnissel;

public interface IObjectMapper
{
    TOut Map<TOut>(RowReader rowReader);
}