using System.Collections.Immutable;
using System.Management.Automation;

namespace MountAnything;

public interface IItem
{
    ItemPath FullPath { get; }
    bool IsContainer { get; }

    string ItemName => FullPath.Name;
    IEnumerable<ItemPath> CacheablePaths
    {
        get { yield return FullPath; }
    }
    IEnumerable<string> Aliases => Enumerable.Empty<string>();
    IDictionary<string, IItem> Links => ImmutableDictionary<string, IItem>.Empty;
    PSObject ToPipelineObject(Func<ItemPath,string> pathResolver);
}

public static class ItemExtensions
{
    public static ItemPath MatchingCacheablePath(this IItem item, ItemPath pathWithPattern)
    {
        return item
            .CacheablePaths
            .FirstOrDefault(path => path.MatchesPattern(pathWithPattern))
            ?? item.FullPath;
    }

    public static bool MatchesPattern(this IItem item, ItemPath pathWithPattern)
    {
        return item.CacheablePaths.Any(p => p.MatchesPattern(pathWithPattern));
    }
}