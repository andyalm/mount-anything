using System.Collections;
using System.Management.Automation;

namespace MountAnything;

public static class PSObjectExtensions
{
    public static PSObject ToPSObject(this object obj)
    {
        return new PSObject(obj);
    }

    public static IEnumerable<PSObject> ToPSObjects<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Select(o => o!.ToPSObject());
    }

    public static T? Property<T>(this PSObject psObject, string propertyName)
    {
        var rawValue = psObject.Properties[propertyName]?.Value;
        if (rawValue == default)
        {
            return default;
        }

        if (rawValue is T typedValue)
        {
            return typedValue;
        }

        if (typeof(T) == typeof(PSObject))
        {
            return (T)(object)rawValue.ToPSObject();
        }

        if (typeof(IEnumerable<PSObject>).IsAssignableFrom(typeof(T)) && rawValue is IEnumerable enumerable)
        {
            return (T)enumerable.Cast<object>().ToPSObjects();
        }

        return (T)Convert.ChangeType(rawValue, typeof(T));
    }

    public static void SetTypeName(this PSObject psObject, string typeName)
    {
        if (!psObject.TypeNames.Contains(typeName))
        {
            psObject.TypeNames.Add(typeName);
        }
        
        // When choosing the right view, we only want powershell to pick the one we specified via the TypeName
        // so we remove all others
        var otherTypeNames = psObject.TypeNames.Where(n => n != typeName).ToArray();
        foreach (var otherTypeName in otherTypeNames)
        {
            psObject.TypeNames.Remove(otherTypeName);
        }
    }

    public static void SetProperty(this PSObject psObject, string propertyName, object? value)
    {
        var property = psObject.Properties[propertyName];
        if (property != null)
        {
            psObject.Properties.Remove(propertyName);
        }
        psObject.Properties.Add(new PSNoteProperty(propertyName, value));
    }

    public static void SetPropertyIfMissing(this PSObject psObject, string propertyName, object value)
    {
        var property = psObject.Properties[propertyName];
        if (property == null)
        {
            psObject.Properties.Add(new PSNoteProperty(propertyName, value));
        }
    }
}