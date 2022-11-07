namespace MountAnything.Content;

public interface IContentWriterHandler
{
    IStreamContentWriter GetContentWriter();
}