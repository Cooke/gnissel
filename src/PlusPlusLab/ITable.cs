namespace PlusPlusLab;

public interface ITable
{
    string Name { get; }

    IReadOnlyCollection<IColumn> Columns { get; }

    Type Type { get; }
}
