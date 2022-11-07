namespace MountAnything.Content;

/// <summary>
/// An implementation of a <see cref="IStreamContentWriter"/> that wraps a <see cref="MemoryStream"/>
/// </summary>
public class StreamContentWriter : IStreamContentWriter
{
    private readonly Action<Stream> _onFinished;

    public StreamContentWriter(Action<Stream> onFinished)
    {
        _onFinished = onFinished;
    }

    public Stream GetWriterStream()
    {
        return new MemoryStream();
    }

    public void WriterFinished(Stream stream)
    {
        _onFinished.Invoke(stream);
    }
}