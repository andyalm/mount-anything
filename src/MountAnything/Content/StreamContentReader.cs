namespace MountAnything.Content;

/// <summary>
/// A simple <see cref="IStreamContentReader"/> implementation that wraps a <see cref="Stream"/>
/// </summary>
public class StreamContentReader : IStreamContentReader
{
    private readonly Stream _stream;
    private readonly Action? _onDispose;

    public StreamContentReader(Stream stream, Action? onDispose = null)
    {
        _stream = stream;
        _onDispose = onDispose;
    }

    public Stream GetContentStream()
    {
        return _stream;
    }
    
    public void Dispose()
    {
        _onDispose?.Invoke();
    }
}