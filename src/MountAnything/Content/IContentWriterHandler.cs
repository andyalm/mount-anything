namespace MountAnything.Content;

public interface IContentWriterHandler
{
    Stream GetWriterStream();
    void WriterFinished(Stream stream);
}