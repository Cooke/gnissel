using System.Data.Common;

namespace PlusPlusLab;

public interface IInsertQuery
{
    ITable Table { get; }

    IReadOnlyCollection<IColumn> Columns { get; }

    IReadOnlyCollection<RowParameters> Rows { get; }
}

public record RowParameters(IReadOnlyCollection<DbParameter> Parameters);
