using System.Collections;
using System.Management.Automation.Provider;
using Autofac;

namespace MountAnything.Content;

internal class StreamContentWriter : IContentWriter
{
    private readonly Stream _stream;
    private readonly IContentWriterHandler _handler;
    private readonly ILifetimeScope _lifetimeScope;
    
    public StreamContentWriter(Stream stream, IContentWriterHandler handler, ILifetimeScope lifetimeScope)
    {
        _stream = stream;
        _handler = handler;
        _lifetimeScope = lifetimeScope;
    }
    
    public void Dispose()
    {
        try
        {
            _stream.Dispose();
        }
        finally
        {
            _lifetimeScope.Dispose();
        }
    }

    public void Close()
    {
        _handler.WriterFinished(_stream);   
    }

    public void Seek(long offset, SeekOrigin origin)
    {
        _stream.Seek(offset, origin);
    }

    public IList Write(IList content)
    {
        var writer = new StreamWriter(_stream);
        foreach (string line in content)
        {
            writer.WriteLine(line);
        }
        writer.Flush();

        return content;
    }
}