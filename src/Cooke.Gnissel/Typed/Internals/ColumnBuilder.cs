using System.Reflection;

namespace Cooke.Gnissel.Typed.Internals;

internal static class ColumnBuilder
{
    public static IEnumerable<Column<T>> CreateColumns<T>(TableOptions options)
    {
        var writer = options.DbOptions.GetWriter<T>();
        foreach (var writeDescriptor in writer.WriteDescriptors)
        {
            if (writeDescriptor is not ColumnWriteDescriptor columnDescriptor)
            {
                throw new NotSupportedException();
            }

            var memberChain = new List<PropertyInfo>(columnDescriptor.PropertyChain.Length);
            var type = typeof(T);
            foreach (var property in columnDescriptor.PropertyChain)
            {
                var propertyInfo = type.GetProperty(property);
                memberChain.Add(propertyInfo ?? throw new InvalidOperationException());
                type = propertyInfo.PropertyType;
            }
            yield return new Column<T>(columnDescriptor.Name, memberChain);
        }
    }
}
