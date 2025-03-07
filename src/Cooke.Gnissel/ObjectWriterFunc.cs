namespace Cooke.Gnissel;

public delegate void ObjectWriterFunc<in T>(T value, IParameterWriter parameterWriter);
