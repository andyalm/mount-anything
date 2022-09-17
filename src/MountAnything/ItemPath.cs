using System.Text.RegularExpressions;

namespace MountAnything;

public class ItemPath
{
    public static explicit operator string(ItemPath path) => path.FullName;
    public static explicit operator ItemPath(string value) => new(value);
    
    public const char Separator = '/';

    public static readonly ItemPath Root = new(string.Empty);
    public string FullName { get; }

    private readonly Lazy<ItemPath> _parent;
    private readonly Lazy<string[]> _parts;
    public ItemPath Parent => _parent.Value;
    public string Name { get; }

    public bool IsRoot => string.IsNullOrEmpty(FullName);

    public ItemPath(string fullName)
    {
        var normalizedPath = fullName.Replace(@"\", Separator.ToString());
        if (normalizedPath.StartsWith(Separator))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        FullName = normalizedPath;
        _parent = new Lazy<ItemPath>(() => new ItemPath(GetParent(normalizedPath)));
        _parts = new Lazy<string[]>(() => normalizedPath.Split(Separator));
        Name = Path.GetFileName(normalizedPath);
    }

    public string[] Parts => _parts.Value;

    public override string ToString()
    {
        return FullName;
    }

    private static string GetParent(string path)
    {
        return Path.GetDirectoryName(path)!.Replace(@"\", Separator.ToString());
    }

    public ItemPath Combine(ItemPath path)
    {
        if (path.IsRoot)
        {
            return this;
        }
        
        return Combine(path.Parts);
    }

    public ItemPath Combine(params string[] parts)
    {
        var combinedPath = Path.Combine(new[] { FullName }.Concat(parts).ToArray()).Replace(@"\", Separator.ToString());
        return new ItemPath(combinedPath);
    }
    
    public ItemPath Ancestor(string ancestorName)
    {
        var currentPath = this;
        while (!currentPath.IsRoot && !currentPath.Name.Equals(ancestorName, StringComparison.OrdinalIgnoreCase))
        {
            currentPath = currentPath.Parent;
        }

        if (currentPath.IsRoot)
        {
            throw new ArgumentException($"No ancestor with name '{ancestorName}' could be found in path {this}");
        }

        return currentPath;
    }
    
    public bool IsAncestorOf(ItemPath otherPath, out string? childPart)
    {
        if (IsRoot)
        {
            childPart = null;
            return false;
        }
        
        if (otherPath.IsRoot)
        {
            childPart = Parts.First();
            return true;
        }

        ItemPath currentPath = Parent;
        childPart = Name;
        while (!currentPath.IsRoot)
        {
            if (currentPath.FullName.Equals(otherPath.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            childPart = currentPath.Name;
            currentPath = currentPath.Parent;
        }

        childPart = null;
        return false;
    }

    public bool MatchesPattern(ItemPath pathWithPattern)
    {
        var patternAsRegex = new Regex("^" + Regex.Escape(pathWithPattern.FullName).Replace(@"\*", ".*") + "$", RegexOptions.IgnoreCase);

        return patternAsRegex.IsMatch(FullName);
    }
    
    public override bool Equals(object other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (ReferenceEquals(this, null)) return false;
        if (ReferenceEquals(other, null)) return false;
        if (GetType() != other.GetType()) return false;
        return Equals(other);
    }
    
    public bool Equals(ItemPath other)
    {
        return FullName == other.FullName;
    }

    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }
}