namespace Nst.Tests;

// Rule 1 on the real program: one call of lines prints at most 120 lines of the file.
public class LinesTests
{
    [Fact]
    public void Lines_prints_at_most_120_lines_however_many_are_asked()
    {
        var file = Path.Combine(Path.GetTempPath(), $"nst-lines-{Guid.NewGuid():N}.txt");
        File.WriteAllLines(file, Enumerable.Range(1, 200).Select(i => $"line {i}"));
        try
        {
            var r = NstProcess.Run("lines", file, "1", "500");
            Assert.Equal(0, r.ExitCode);
            Assert.Equal(120, r.OutLines.Length);
            Assert.Equal("1: line 1", r.OutLines[0]);
            Assert.Equal("120: line 120", r.OutLines[^1]);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
