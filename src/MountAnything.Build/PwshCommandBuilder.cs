using System.Text;
using Microsoft.Build.Framework;

namespace MountAnything.Build;

internal static class PwshCommandBuilder
{
    public static void AddParameter(this StringBuilder builder, string parameterName, ITaskItem? item)
    {
        if (item != null)
        {
            builder.Append($" -{parameterName} \"{item.FullPath()}\"");
        }
    }
    
    public static void AddParameter(this StringBuilder builder, string parameterName, string? value)
    {
        if (value != null)
        {
            builder.Append($" -{parameterName} \"{value}\"");
        }
    }
}