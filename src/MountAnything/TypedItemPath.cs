namespace MountAnything;

public abstract class TypedItemPath
{
    protected TypedItemPath(ItemPath path)
    {
        Path = path;
    }
    
    public ItemPath Path { get; }

    public override string ToString()
    {
        return Path.ToString();
    }
    protected bool Equals(TypedItemPath other)
    {
        return Path.Equals(other.Path);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TypedItemPath)obj);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }

    public static bool operator ==(TypedItemPath? left, TypedItemPath? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypedItemPath? left, TypedItemPath? right)
    {
        return !Equals(left, right);
    }
}