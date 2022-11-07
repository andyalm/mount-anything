namespace MountAnything.Content;

public interface IContentStreamWriter
{
    Stream GetWriterStream();
    
    void WriterFinished(Stream stream);
}