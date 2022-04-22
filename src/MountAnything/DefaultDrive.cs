namespace MountAnything;

public record DefaultDrive(string Name)
{
    public string? Description { get; init; }
}