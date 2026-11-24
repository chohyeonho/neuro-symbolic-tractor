using Nst.Application.Ports;

namespace Nst.Infrastructure;

// Fills ITextFiles with the real file system, inside one project folder.
// A path is followed through every symbolic link and junction first, and is read only when where it really
// leads is a regular file inside the folder. So a session that may run nst cannot read files outside the
// folder it was launched in, such as keys in the home folder.
// Every failure to open the file becomes an IOException, as the port promises.
public sealed class FileSystemTextFiles(string projectFolder) : ITextFiles
{
    static readonly StringComparison PathCase =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    readonly string root = RealPath(projectFolder);

    public IEnumerable<string> ReadLines(string path)
    {
        try
        {
            var real = RealPath(Path.GetFullPath(path, root));
            if (!Inside(real))
                throw new IOException($"{path} is outside the project folder.");
            if (!File.Exists(real) && !Directory.Exists(real))
                throw new FileNotFoundException($"{path} was not found.");
            if (!IsRegularFile(real))
                throw new IOException($"{path} is not a regular file.");
            return File.ReadLines(real);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new IOException(ex.Message, ex);
        }
    }

    bool Inside(string real) =>
        real.StartsWith(root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar, PathCase);

    // A file that is not a folder, a device or a link. Windows names devices and pipes outside
    // any folder (\\.\), so the folder check already refuses them.
    static bool IsRegularFile(string real)
    {
        if (!File.Exists(real)) return false;
        var attributes = File.GetAttributes(real);
        return (attributes & (FileAttributes.Directory | FileAttributes.Device | FileAttributes.ReparsePoint)) == 0;
    }

    // The full path with every link on the way replaced by where it finally leads.
    // A link that leads to another link is followed again, up to a depth that only a loop reaches.
    static string RealPath(string path, int depth = 0)
    {
        if (depth > 40) throw new IOException("Too many links on the way to the file.");
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full)!;
        foreach (var part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var next = Path.Combine(current, part);
            FileSystemInfo entry = Directory.Exists(next) ? new DirectoryInfo(next) : new FileInfo(next);
            if (entry.Exists && entry.LinkTarget is not null)
                next = RealPath(entry.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? next, depth + 1);
            current = next;
        }
        return current;
    }
}
