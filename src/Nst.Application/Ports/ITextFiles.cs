namespace Nst.Application.Ports;

// Port: reading a text file as its lines.
public interface ITextFiles
{
    // The lines of the file at path, read lazily. Throws IOException when the file cannot be read.
    IEnumerable<string> ReadLines(string path);
}
