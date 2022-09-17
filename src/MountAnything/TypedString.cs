namespace MountAnything;

public abstract class TypedString
{
    protected TypedString(string value)
    {
        Value = value;
    }
    
    public string Value { get; }
    
    public static implicit operator string(TypedString typedString) => typedString.Value;

    public override string ToString()
    {
        return Value;
    }

    protected bool Equals(TypedString other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TypedString)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TypedString? left, TypedString? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypedString? left, TypedString? right)
    {
        return !Equals(left, right);
    }
}