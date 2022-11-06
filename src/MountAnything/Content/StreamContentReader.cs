using System.Collections;
using System.Management.Automation.Provider;
using Autofac;

namespace MountAnything.Content;

internal class StreamContentReader : IContentReader
{
    private readonly Stream _contentStream;
    private readonly StreamReader _reader;
    private readonly ILifetimeScope _lifetimeScope;
    private readonly IPathHandlerContext _context;

    public StreamContentReader(Stream contentStream, ILifetimeScope lifetimeScope, IPathHandlerContext context)
    {
        _contentStream = contentStream;
        _reader = new StreamReader(contentStream);
        _lifetimeScope = lifetimeScope;
        _context = context;
    }

    public void Dispose()
    {
        try
        {
            _contentStream.Dispose();
        }
        finally
        {
            _lifetimeScope.Dispose();
        }
    }

    public void Close()
    {
        _contentStream.Close();
    }

    public IList Read(long readCount)
    {
        _context.WriteDebug($"StreamContentReader.Read({readCount})");
        var blocks = new List<string>();
        
        while (!_reader.EndOfStream && blocks.Count < readCount)
        {
            var line = _reader.ReadLine();
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
        _contentStream.Seek(offset, origin);
    }
}