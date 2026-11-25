using Microsoft.Win32.SafeHandles;
using Nst.Application.Ports;

namespace Nst.Infrastructure;

// Fills ITextFiles with the real file system, inside one project folder.
// The folder is given by whoever launches the session (nst reads it from NST_ROOT), never by the folder a call
// happens to run from, so a call made elsewhere cannot widen what nst reads.
// A file is read only when the opened file itself is a regular file inside the folder: the system is asked about
// the open handle, so swapping a link or a folder between the checks and the read cannot point nst elsewhere.
// Every failure to open the file becomes an IOException, as the port promises.
public sealed class FileSystemTextFiles(string? projectFolder) : ITextFiles
{
    // A quick first check on the name, before anything is opened. On Windows it ignores case, as names
    // usually do there; the check on the opened file below compares the names exactly as they are on disk.
    static readonly StringComparison NameCase =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    readonly Lazy<(string Real, string Final)> root = new(() =>
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
            throw new IOException("No project folder was given to read from.");
        // A relative folder would be taken from wherever each call runs, which is what NST_ROOT is there to stop.
        if (!Path.IsPathFullyQualified(projectFolder))
            throw new IOException($"The project folder must be a full path, not \"{projectFolder}\".");
        return (RealPath(projectFolder), OpenedFile.FinalPathOfFolder(projectFolder));
    });

    // Runs between the checks on the name and the open. Tests use it to swap a link at the worst moment.
    internal Action? BetweenCheckAndOpen { get; init; }

    public IEnumerable<string> ReadLines(string path)
    {
        try
        {
            var (rootReal, rootFinal) = root.Value;
            var real = RealPath(Path.GetFullPath(path, rootReal));
            if (!Inside(rootReal, real, NameCase))
                throw new IOException($"{path} is outside the project folder.");
            if (!File.Exists(real) && !Directory.Exists(real))
                throw new FileNotFoundException($"{path} was not found.");
            if (!IsRegularFile(real))
                throw new IOException($"{path} is not a regular file.");

            BetweenCheckAndOpen?.Invoke();
            var (handle, final) = OpenedFile.OpenRegular(real, path);
            if (!Inside(rootFinal, final, StringComparison.Ordinal))
            {
                handle.Dispose();
                throw new IOException($"{path} is outside the project folder.");
            }
            return LinesOf(handle);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new IOException(ex.Message, ex);
        }
    }

    static IEnumerable<string> LinesOf(SafeFileHandle handle)
    {
        using var reader = new StreamReader(new FileStream(handle, FileAccess.Read));
        while (reader.ReadLine() is { } line) yield return line;
    }

    static bool Inside(string root, string real, StringComparison comparison) =>
        real.StartsWith(root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar, comparison);

    // A file that is not a folder, a device or a link. Windows device names never get this far. NUL becomes
    // \\.\NUL, outside any folder, so the folder check refuses it. Since Windows 11, CON, CONIN$, COM1 and the like
    // inside a folder are plain names that no file has, so the check that the file exists refuses them.
    // The open checks the kind again on the handle. On Linux that is what refuses a POSIX FIFO; on Windows no name
    // inside a folder is known to reach it, so it stays as a second guard that no test covers there.
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
