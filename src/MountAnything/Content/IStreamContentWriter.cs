namespace MountAnything.Content;

public interface IStreamContentWriter
{
    Stream GetWriterStream();
    
    void WriterFinished(Stream stream);
}