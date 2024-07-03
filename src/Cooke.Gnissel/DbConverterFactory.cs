﻿namespace Cooke.Gnissel;

public abstract class DbConverterFactory : DbConverter
{
    public abstract bool CanCreateFor(Type type);

    public abstract ConcreteDbConverter Create(Type type);
}
