namespace MountAnything.Content;

public class EmptyContentReader : IContentStreamReader
{
    public void Dispose()
    {
        
    }

    public Stream GetContentStream()
    {
        return new MemoryStream();
    }
}