namespace MountAnything.Hosting.Abstractions;

public static class ProviderHostAccessor
{
    private static readonly AsyncLocal<IProviderHost> _currentHost = new();

    public static IProviderHost Current
    {
        get => _currentHost.Value ?? throw new InvalidOperationException("No current IProviderHost has been set");
        set => _currentHost.Value = value;
    }
}