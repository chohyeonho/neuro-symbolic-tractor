using System.Diagnostics;
using Nst.Application.Actions;
using Nst.Infrastructure;

namespace Nst.Tests;

// nst lines on the built program (nst.dll) as a process, run from a project folder made for each test.
// Rule 1: one call prints at most 120 lines of the file. These checks run the C# program; the Lean proof
// checks its model on its own, so the two are independent and neither reads the other.
public sealed class LinesTests : IDisposable
{
    readonly string top = Path.Combine(Path.GetTempPath(), $"nst-lines-{Guid.NewGuid():N}");
    readonly string project;
    readonly string outside;

    public LinesTests()
    {
        project = Directory.CreateDirectory(Path.Combine(top, "project")).FullName;
        outside = Directory.CreateDirectory(Path.Combine(top, "outside")).FullName;
    }

    public void Dispose()
    {
        foreach (var link in Directory.EnumerateFileSystemEntries(project).Where(e => new FileInfo(e).LinkTarget is not null
                     || new DirectoryInfo(e).LinkTarget is not null))
            Directory.Delete(link);
        Directory.Delete(top, recursive: true);
    }

    string Lines(string name, int count)
    {
        File.WriteAllLines(Path.Combine(project, name), Enumerable.Range(1, count).Select(i => $"line {i}"));
        return name;
    }

    [Fact]
    public void Lines_prints_at_most_120_lines_however_many_are_asked()
    {
        var r = NstProcess.RunIn(project, "lines", Lines("a.txt", 200), "1", "500");
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(120, r.OutLines.Length);
        Assert.Equal("1: line 1", r.OutLines[0]);
        Assert.Equal("120: line 120", r.OutLines[^1]);
    }

    [Fact]
    public void Lines_without_a_count_prints_120_lines()
    {
        var r = NstProcess.RunIn(project, "lines", Lines("a.txt", 200), "50");
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(120, r.OutLines.Length);
        Assert.Equal("50: line 50", r.OutLines[0]);
        Assert.Equal("169: line 169", r.OutLines[^1]);
    }

    [Fact]
    public void Lines_from_a_later_start_prints_the_count_asked()
    {
        var r = NstProcess.RunIn(project, "lines", Lines("a.txt", 200), "150", "10");
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(Enumerable.Range(150, 10).Select(i => $"{i}: line {i}"), r.OutLines);
    }

    [Fact]
    public void Lines_prints_a_short_file_whole()
    {
        var r = NstProcess.RunIn(project, "lines", Lines("short.txt", 10), "1", "120");
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(10, r.OutLines.Length);
    }

    [Fact]
    public void Lines_treats_a_count_too_large_for_a_number_as_the_cap()
    {
        var r = NstProcess.RunIn(project, "lines", Lines("a.txt", 200), "1", "99999999999999999999");
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(120, r.OutLines.Length);
    }

    [Fact]
    public void Lines_from_a_start_too_large_for_a_number_prints_nothing()
    {
        var r = NstProcess.RunIn(project, "lines", Lines("a.txt", 5), "99999999999999999999");
        Assert.Equal(0, r.ExitCode);
        Assert.Empty(r.OutLines);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("+3")]
    [InlineData("3.5")]
    [InlineData("three")]
    public void Lines_refuses_a_start_that_is_not_a_whole_number_of_1_or_more(string start)
    {
        var r = NstProcess.RunIn(project, "lines", Lines("a.txt", 5), start);
        Assert.Equal(2, r.ExitCode);
        Assert.Empty(r.OutLines);
    }

    [Fact]
    public void Lines_reports_a_missing_file()
    {
        var r = NstProcess.RunIn(project, "lines", "missing.txt", "1");
        Assert.Equal(1, r.ExitCode);
        Assert.Empty(r.OutLines);
        Assert.Contains("missing.txt was not found", r.Err);
    }

    [Fact]
    public void Lines_refuses_a_folder()
    {
        Directory.CreateDirectory(Path.Combine(project, "sub"));
        var r = NstProcess.RunIn(project, "lines", "sub", "1");
        Assert.Equal(1, r.ExitCode);
        Assert.Contains("not a regular file", r.Err);
    }

    const string Secret = "DUMMY-CREDENTIAL-0123456789";

    string KeyOutside()
    {
        var key = Path.Combine(outside, "api_key");
        File.WriteAllText(key, Secret);
        return key;
    }

    void AssertRefusedOutside(NstProcess.Result r)
    {
        Assert.Equal(1, r.ExitCode);
        Assert.Empty(r.OutLines);
        Assert.Contains("outside the project folder", r.Err);
        Assert.DoesNotContain(Secret, r.Err);
    }

    [Fact]
    public void Lines_refuses_a_file_outside_the_project_folder_by_full_path() =>
        AssertRefusedOutside(NstProcess.RunIn(project, "lines", KeyOutside(), "1"));

    [Fact]
    public void Lines_refuses_a_file_outside_the_project_folder_by_climbing_up()
    {
        KeyOutside();
        AssertRefusedOutside(NstProcess.RunIn(project, "lines", Path.Combine("..", "outside", "api_key"), "1"));
    }

    [Fact]
    public void Lines_refuses_a_link_in_the_project_folder_that_leads_outside()
    {
        KeyOutside();
        LinkFolder(Path.Combine(project, "escape"), outside);
        AssertRefusedOutside(NstProcess.RunIn(project, "lines", Path.Combine("escape", "api_key"), "1"));
    }

    // A device name is refused by a check on the name, before anything is opened: one that the system maps to
    // \\.\ (NUL, and on Windows before 11 the others too) is outside the folder; one it leaves as a plain name
    // inside the folder is not found.
    [Theory]
    [InlineData("CON")]
    [InlineData("CONIN$")]
    [InlineData("COM1")]
    [InlineData("NUL")]
    public void Lines_refuses_a_windows_device_name_at_once(string name)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Device names exist only on Windows.");
        var refusal = Path.GetFullPath(Path.Combine(project, name)).StartsWith(@"\\.\", StringComparison.Ordinal)
            ? "is outside the project folder"
            : "was not found";
        var watch = Stopwatch.StartNew();
        var r = NstProcess.RunIn(project, "lines", name, "1");
        Assert.Equal(1, r.ExitCode);
        Assert.Empty(r.OutLines);
        Assert.Contains($"{name} {refusal}", r.Err);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
    }

    // A link to a folder. A junction needs no special right on Windows, unlike a symbolic link.
    static void LinkFolder(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            using var mklink = Process.Start(new ProcessStartInfo("cmd", ["/c", "mklink", "/J", link, target])
                { UseShellExecute = false, RedirectStandardOutput = true })!;
            mklink.WaitForExit();
            Assert.Equal(0, mklink.ExitCode);
        }
        else
        {
            Directory.CreateSymbolicLink(link, target);
        }
    }

    [Fact]
    public void Lines_reads_from_the_folder_the_launcher_set_not_from_where_it_is_run()
    {
        KeyOutside();
        // Run from above the project and from the folder holding the key, a relative name still means a file in
        // the project, where there is none.
        foreach (var (folder, name) in new[] { (top, Path.Combine("outside", "api_key")), (outside, "api_key") })
        {
            var r = NstProcess.RunWith(project, folder, "lines", name, "1");
            Assert.Equal(1, r.ExitCode);
            Assert.Contains("was not found", r.Err);
            Assert.DoesNotContain(Secret, string.Join('\n', r.OutLines) + r.Err);
        }
    }

    [Fact]
    public void Lines_refuses_to_read_when_the_launcher_set_no_folder()
    {
        Lines("a.txt", 3);
        var r = NstProcess.RunWith(null, project, "lines", "a.txt", "1");
        Assert.Equal(1, r.ExitCode);
        Assert.Empty(r.OutLines);
        Assert.Contains("No project folder was given", r.Err);
    }

    [Fact]
    public void Lines_refuses_a_project_folder_that_is_not_a_full_path()
    {
        Lines("a.txt", 3);
        var r = NstProcess.RunWith(".", project, "lines", "a.txt", "1");
        Assert.Equal(1, r.ExitCode);
        Assert.Empty(r.OutLines);
        Assert.Contains("The project folder must be a full path", r.Err);
    }

    [Fact]
    public void Lines_refuses_a_sibling_folder_whose_name_differs_only_in_case()
    {
        var parent = Directory.CreateDirectory(Path.Combine(top, "cased")).FullName;
        if (OperatingSystem.IsWindows())
        {
            // Windows lets one folder tell names apart by case, as Linux does; it must be set while empty.
            using var fsutil = Process.Start(new ProcessStartInfo("fsutil", ["file", "setCaseSensitiveInfo", parent, "enable"])
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true })!;
            fsutil.WaitForExit();
            Assert.SkipUnless(fsutil.ExitCode == 0, "This Windows cannot make a folder case-sensitive.");
        }
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "nst lines does not read files on macOS.");
        var allowed = Directory.CreateDirectory(Path.Combine(parent, "proj")).FullName;
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(parent, "Proj")).FullName, "api_key"), Secret);

        AssertRefusedOutside(NstProcess.RunWith(allowed, allowed, "lines", Path.Combine("..", "Proj", "api_key"), "1"));
    }

    [Fact]
    public void Lines_refuses_a_folder_swapped_for_a_link_after_the_checks()
    {
        KeyOutside();
        var inner = Directory.CreateDirectory(Path.Combine(project, "inner")).FullName;
        File.WriteAllText(Path.Combine(inner, "api_key"), "harmless");
        var files = new FileSystemTextFiles(project)
        {
            // Another process in the project replaces the folder with a link out, after the name was checked.
            BetweenCheckAndOpen = () =>
            {
                Directory.Delete(inner, recursive: true);
                LinkFolder(inner, outside);
            },
        };

        var error = Assert.Throws<IOException>(() => files.ReadLines(Path.Combine("inner", "api_key")).ToList());
        Assert.Contains("outside the project folder", error.Message);
        Assert.DoesNotContain(Secret, error.ToString());
    }

    [Fact]
    public void Lines_refuses_a_posix_fifo_at_once()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "FIFOs in a folder are a POSIX thing; this checks Linux.");
        var fifo = Path.Combine(project, "pipe");
        using (var mkfifo = Process.Start(new ProcessStartInfo("mkfifo", [fifo]) { UseShellExecute = false })!)
        {
            mkfifo.WaitForExit();
            Assert.Equal(0, mkfifo.ExitCode);
        }

        // NstProcess kills the call and fails the test if it does not return in time.
        var r = NstProcess.RunIn(project, "lines", "pipe", "1");
        Assert.Equal(1, r.ExitCode);
        Assert.Contains("not a regular file", r.Err);
    }

    [Fact]
    public void Help_lists_the_lines_action()
    {
        var r = NstProcess.RunIn(project);
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("  " + new LinesAction(new FileSystemTextFiles(project)).Usage, r.OutLines);
    }
}
