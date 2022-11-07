namespace MountAnything.Content;

public interface IContentStreamReader : IDisposable
{
    Stream GetContentStream();
}