using System.Collections;
using System.Management.Automation.Provider;
using Autofac;

namespace MountAnything.Content;

internal class ContentReader : IContentReader
{
    private readonly IStreamContentReader _contentReader;
    private readonly Lazy<Stream> _stream;
    private readonly Lazy<StreamReader> _streamReader;
    private readonly ILifetimeScope _lifetimeScope;
    private readonly IPathHandlerContext _context;

    public ContentReader(IStreamContentReader reader, ILifetimeScope lifetimeScope, IPathHandlerContext context)
    {
        _contentReader = reader;
        _stream = new Lazy<Stream>(() => _contentReader.GetContentStream());
        _streamReader = new Lazy<StreamReader>(() => new StreamReader(_stream.Value));
        _lifetimeScope = lifetimeScope;
        _context = context;
    }

    public void Dispose()
    {
        try
        {
            _contentReader.Dispose();
        }
        finally
        {
            _lifetimeScope.Dispose();
        }
    }

    public void Close()
    {
        if (_streamReader.IsValueCreated)
        {
            _streamReader.Value.Dispose();
        }
    }

    public IList Read(long readCount)
    {
        _context.WriteDebug($"StreamContentReader.Read({readCount})");
        var blocks = new List<string>();
        
        while (!_streamReader.Value.EndOfStream && blocks.Count < readCount)
        {
            var line = _streamReader.Value.ReadLine();
            if (line != null)
            {
                blocks.Add(line);
            }
            else
            {
                break;
            }
        }

        return blocks;
    }

    public void Seek(long offset, SeekOrigin origin)
    {
        _context.WriteDebug($"StreamContentReader.Seek({offset}, {origin})");
        _stream.Value.Seek(offset, origin);
    }
}