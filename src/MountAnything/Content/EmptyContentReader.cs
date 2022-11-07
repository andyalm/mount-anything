namespace MountAnything.Content;

public class EmptyContentReader : IStreamContentReader
{
    public void Dispose()
    {
        
    }

    public Stream GetContentStream()
    {
        return new MemoryStream();
    }
}