namespace MountAnything.Content;

public interface IStreamContentReader : IDisposable
{
    Stream GetContentStream();
}