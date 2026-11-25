using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Nst.Infrastructure;

// Opens a file and asks the operating system what the open handle is: its kind, and its full path with every
// link followed and every name spelled as it is on disk. The answers are about the handle, not the name,
// so nothing can change them between the question and the read.
// Windows and Linux only; on any other system nothing is opened.
static partial class OpenedFile
{
    // The open file and its final path. Throws IOException when it is not a regular file; shown is the path
    // as the caller gave it.
    public static (SafeFileHandle Handle, string FinalPath) OpenRegular(string path, string shown)
    {
        var handle = Open(path);
        try
        {
            if (!IsRegular(handle)) throw new IOException($"{shown} is not a regular file.");
            return (handle, FinalPath(handle));
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    // The final path of a folder, in the same form OpenRegular gives for files.
    public static string FinalPathOfFolder(string folder)
    {
        using var handle = OperatingSystem.IsWindows() ? Windows.OpenFolder(folder) : Open(folder);
        return FinalPath(handle);
    }

    static SafeFileHandle Open(string path) =>
        OperatingSystem.IsWindows() ? File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)
        : OperatingSystem.IsLinux() ? Linux.Open(path)
        : throw new IOException("nst reads files on Windows and Linux only.");

    static bool IsRegular(SafeFileHandle handle) => OperatingSystem.IsWindows() ? Windows.IsDisk(handle) : Linux.IsRegular(handle);

    static string FinalPath(SafeFileHandle handle) => OperatingSystem.IsWindows() ? Windows.FinalPath(handle) : Linux.FinalPath(handle);

    static IOException Failed(string what) => new(what, new Win32Exception(Marshal.GetLastPInvokeError()));

    static partial class Windows
    {
        const uint FileTypeDisk = 1;
        const uint OpenExisting = 3;
        const uint ShareAll = 7;
        const uint BackupSemantics = 0x02000000; // lets CreateFile open a folder

        public static SafeFileHandle OpenFolder(string folder)
        {
            var handle = CreateFileW(folder, 0, ShareAll, 0, OpenExisting, BackupSemantics, 0);
            if (handle.IsInvalid) throw Failed($"The project folder {folder} cannot be opened.");
            return handle;
        }

        // A disk file; a console, a pipe or a serial port is something else.
        public static bool IsDisk(SafeFileHandle handle) => GetFileType(handle) == FileTypeDisk;

        public static string FinalPath(SafeFileHandle handle)
        {
            var buffer = new char[512];
            while (true)
            {
                var length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Length, 0);
                if (length == 0) throw Failed("The system did not say where the file is.");
                if (length < buffer.Length) return WithoutPrefix(new string(buffer, 0, (int)length));
                buffer = new char[length];
            }
        }

        // \\?\C:\x is C:\x, and \\?\UNC\server\share is \\server\share.
        static string WithoutPrefix(string path) =>
            path.StartsWith(@"\\?\UNC\", StringComparison.Ordinal) ? @"\\" + path[8..]
            : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..]
            : path;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial uint GetFileType(SafeFileHandle handle);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint GetFinalPathNameByHandleW(SafeFileHandle handle, [Out] char[] path, uint length, uint flags);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);
    }

    static partial class Linux
    {
        const int ReadOnly = 0;
        const int NonBlocking = 0x800; // O_NONBLOCK: a FIFO opens at once instead of waiting for a writer
        const int CloseOnExec = 0x80000;
        const int EmptyPath = 0x1000; // AT_EMPTY_PATH: statx describes the open handle itself
        const uint StatxType = 1;
        const int TypeMask = 0xF000;
        const int RegularType = 0x8000;
        const int StatxModeOffset = 0x1C; // stx_mode in struct statx, the same on every Linux

        public static SafeFileHandle Open(string path)
        {
            var fd = open(path, ReadOnly | NonBlocking | CloseOnExec);
            if (fd < 0) throw Failed($"{path} cannot be opened.");
            return new SafeFileHandle(fd, ownsHandle: true);
        }

        public static bool IsRegular(SafeFileHandle handle)
        {
            var statx = new byte[256];
            if (Statx((int)handle.DangerousGetHandle(), "", EmptyPath, StatxType, statx) != 0)
                throw Failed("The system did not say what the file is.");
            return (BitConverter.ToUInt16(statx, StatxModeOffset) & TypeMask) == RegularType;
        }

        public static string FinalPath(SafeFileHandle handle)
        {
            var buffer = new byte[4096];
            var length = readlink($"/proc/self/fd/{(int)handle.DangerousGetHandle()}", buffer, buffer.Length);
            if (length < 0 || length == buffer.Length) throw Failed("The system did not say where the file is.");
            return Encoding.UTF8.GetString(buffer, 0, (int)length);
        }

        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int open(string path, int flags);

        [LibraryImport("libc", EntryPoint = "statx", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int Statx(int dirfd, string path, int flags, uint mask, [Out] byte[] statx);

        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint readlink(string path, [Out] byte[] buffer, nint size);
    }
}
