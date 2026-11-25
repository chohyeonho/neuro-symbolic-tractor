namespace Nst.Tests.Judging;

// Which judge fills the slot. To swap models, add an IJudge adapter and change this one place.
public static class Judges
{
    // The configured judge, or null when no key is set up (tests that need a judge are then skipped).
    public static IJudge? Current()
    {
        var key = JevKey();
        return key is null ? null : new JevJudge(key);
    }

    // From JEV_API_KEY, else ~/.config/typesafe/api_key. CI sets JEV_API_KEY from the repository secret.
    static string? JevKey() => KeyFrom(Environment.GetEnvironmentVariable("JEV_API_KEY"), ReadKeyFile);

    static string? ReadKeyFile()
    {
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "typesafe", "api_key");
        return File.Exists(file) ? File.ReadAllText(file) : null;
    }

    // The variable wins when it holds a key, and then the file is not read at all.
    // A blank variable or a blank file counts as no key.
    public static string? KeyFrom(string? variable, Func<string?> readFile)
    {
        if (!string.IsNullOrWhiteSpace(variable)) return variable.Trim();
        var fileText = readFile();
        return string.IsNullOrWhiteSpace(fileText) ? null : fileText.Trim();
    }
}
