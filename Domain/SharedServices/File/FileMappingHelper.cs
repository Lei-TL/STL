using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace STL.SharedServices.File;

internal static class FileMappingHelper
{
    public static List<PropertyInfo> GetWritableProperties<T>()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanWrite)
            .ToList();
    }

    public static List<PropertyInfo> GetReadableProperties<T>()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.CanRead
                && property.GetIndexParameters().Length == 0
                && property.PropertyType != typeof(string)
                && !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            .ToList();
    }

    public static T CreateItem<T>()
    {
        return Activator.CreateInstance<T>();
    }

    public static void SetPropertyValue<T>(
        T item,
        PropertyInfo property,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType)
            ?? property.PropertyType;
        object? convertedValue;

        if (targetType.IsEnum)
        {
            convertedValue = Enum.Parse(targetType, value, ignoreCase: true);
        }
        else if (targetType == typeof(Guid))
        {
            convertedValue = Guid.Parse(value);
        }
        else
        {
            var converter = TypeDescriptor.GetConverter(targetType);
            convertedValue = converter.ConvertFromInvariantString(value);
        }

        property.SetValue(item, convertedValue);
    }

    public static string GetPropertyValue<T>(T item, PropertyInfo property)
    {
        var value = property.GetValue(item);

        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, null),
            _ => value.ToString() ?? string.Empty
        };
    }

    public static Dictionary<string, PropertyInfo> CreatePropertyMap<T>(
        IEnumerable<PropertyInfo> properties)
    {
        return properties.ToDictionary(
            property => property.Name,
            property => property,
            StringComparer.OrdinalIgnoreCase);
    }
}
