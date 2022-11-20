namespace MountAnything.Content;

public class HttpResponseContentReader : IStreamContentReader
{
    private readonly HttpResponseMessage _response;

    public HttpResponseContentReader(HttpResponseMessage response)
    {
        _response = response;
    }

    public void Dispose()
    {
        _response.Dispose();
    }

    public Stream GetContentStream()
    {
        return _response.Content.ReadAsStream();
    }
}