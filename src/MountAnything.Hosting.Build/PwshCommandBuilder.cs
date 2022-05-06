using System.Text;
using Microsoft.Build.Framework;

namespace MountAnything.Hosting.Build;

internal static class PwshCommandBuilder
{
    public static void AddParameter(this StringBuilder builder, string parameterName, ITaskItem? item)
    {
        if (item != null)
        {
            builder.Append($" -{parameterName} \"{item.ItemSpec}\"");
        }
    }
    public static void AddParameter(this StringBuilder builder, string parameterName, ICollection<ITaskItem> items)
    {
        var serializedItemArray = string.Join(",", items.Select(i => $"\"{i.ItemSpec}\""));
        builder.Append($" -{parameterName} @({serializedItemArray})");
    }
    
    public static void AddParameter(this StringBuilder builder, string parameterName, ICollection<string> values)
    {
        var serializedArray = string.Join(",", values.Select(v => $"\"{v}\""));
        builder.Append($" -{parameterName} @({serializedArray})");
    }
    
    public static void AddParameter(this StringBuilder builder, string parameterName, string? value)
    {
        if (value != null)
        {
            builder.Append($" -{parameterName} \"{value}\"");
        }
    }
}