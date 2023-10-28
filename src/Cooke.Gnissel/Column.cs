#region

using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel;

public class Column<TTable>
{
    private readonly Func<TTable, DbParameter> _parameterFactory;

    public Column(IDbAdapter dbAdapter, string name, bool isIdentity, MemberInfo member)
    {
        Name = name;
        IsIdentity = isIdentity;
        Member = member;

        var tableItemParameter = Expression.Parameter(typeof(TTable));
        var propertyInfo = (PropertyInfo)member;
        _parameterFactory =
            (Func<TTable, DbParameter>)
                Expression
                    .Lambda(
                        Expression.GetFuncType(typeof(TTable), typeof(DbParameter)),
                        Expression.Call(
                            Expression.Constant(dbAdapter),
                            nameof(dbAdapter.CreateParameter),
                            new[] { propertyInfo.PropertyType },
                            Expression.Property(tableItemParameter, propertyInfo),
                            Expression.Constant(GetDbType(propertyInfo), typeof(string))
                        ),
                        tableItemParameter
                    )
                    .Compile();
    }

    private string? GetDbType(PropertyInfo propertyInfo)
    {
        var dataTypeAttribute = propertyInfo.GetCustomAttribute<DataTypeAttribute>();
        return dataTypeAttribute?.CustomDataType;
    }

    public DbParameter CreateParameter(TTable item) => _parameterFactory(item);

    public string Name { get; }

    public bool IsIdentity { get; }

    public MemberInfo Member { get; }
}
