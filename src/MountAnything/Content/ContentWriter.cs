using System.Collections;
using System.Management.Automation.Provider;
using Autofac;

namespace MountAnything.Content;

internal class ContentWriter : IContentWriter
{
    private readonly IStreamContentWriter _writer;
    private readonly Lazy<Stream> _stream;
    private readonly ILifetimeScope _lifetimeScope;
    
    public ContentWriter(IStreamContentWriter writer, ILifetimeScope lifetimeScope)
    {
        _writer = writer;
        _stream = new Lazy<Stream>(writer.GetWriterStream);
        _writer = writer;
        _lifetimeScope = lifetimeScope;
    }
    
    public void Dispose()
    {
        try
        {
            if (_stream.IsValueCreated)
            {
                _stream.Value.Dispose();
            }
        }
        finally
        {
            _lifetimeScope.Dispose();
        }
    }

    public void Close()
    {
        _writer.WriterFinished(_stream.Value);   
    }

    public void Seek(long offset, SeekOrigin origin)
    {
        _stream.Value.Seek(offset, origin);
    }

    public IList Write(IList content)
    {
        var writer = new StreamWriter(_stream.Value);
        foreach (string line in content)
        {
            writer.WriteLine(line);
        }
        writer.Flush();

        return content;
    }
}