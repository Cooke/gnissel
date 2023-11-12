namespace Cooke.Gnissel;

public readonly record struct ObjectReader<TOut>(ObjectReaderFunc<TOut> Read, int Width);