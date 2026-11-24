using System.Diagnostics;
using Nst.Application.Actions;
using Nst.Infrastructure;

namespace Nst.Tests;

// nst lines on the real program, run from a project folder made for each test.
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
        var link = Path.Combine(project, "escape");
        if (OperatingSystem.IsWindows())
        {
            // A junction needs no special right on Windows, unlike a symbolic link.
            using var mklink = Process.Start(new ProcessStartInfo("cmd", ["/c", "mklink", "/J", link, outside])
                { UseShellExecute = false, RedirectStandardOutput = true })!;
            mklink.WaitForExit();
            Assert.Equal(0, mklink.ExitCode);
        }
        else
        {
            Directory.CreateSymbolicLink(link, outside);
        }

        AssertRefusedOutside(NstProcess.RunIn(project, "lines", Path.Combine("escape", "api_key"), "1"));
    }

    [Fact]
    public void Lines_refuses_a_windows_device_at_once()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Device names exist only on Windows.");
        var watch = Stopwatch.StartNew();
        var r = NstProcess.RunIn(project, "lines", "CON", "1");
        Assert.Equal(1, r.ExitCode);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
    }

    [Fact]
    public void Help_lists_the_lines_action()
    {
        var r = NstProcess.RunIn(project);
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("  " + new LinesAction(new FileSystemTextFiles(project)).Usage, r.OutLines);
    }
}
