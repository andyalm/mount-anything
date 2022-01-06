using System.Management.Automation;

namespace MountAnything;

public static class ItemPropertyExtensions
{
    public static IEnumerable<IItemProperty> AsItemProperties(this PSObject psObject)
    {
        foreach (var property in psObject.Properties.Where(p => p.IsGettable))
        {
            yield return new PSMemberProperty(property);
        }
    }

    public static IEnumerable<IItemProperty> WherePropertiesMatch(this IEnumerable<IItemProperty> itemProperties, HashSet<string> propertyNames)
    {
        return itemProperties.Where(p => propertyNames.Count == 0 || propertyNames.Contains(p.Name));
    }
}

public class PSMemberProperty : IItemProperty
{
    public PSPropertyInfo Property { get; }

    public PSMemberProperty(PSPropertyInfo property)
    {
        Property = property;
    }

    public string Name => Property.Name;
    public object Value => Property.Value;
}