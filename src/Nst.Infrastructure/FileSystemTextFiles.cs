using Nst.Application.Ports;

namespace Nst.Infrastructure;

// Fills ITextFiles with the real file system. Every failure to open the file becomes an IOException,
// as the port promises.
public sealed class FileSystemTextFiles : ITextFiles
{
    public IEnumerable<string> ReadLines(string path)
    {
        try
        {
            return File.ReadLines(path);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new IOException(ex.Message, ex);
        }
    }
}
