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
    static string? JevKey()
    {
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "typesafe", "api_key");
        return KeyFrom(Environment.GetEnvironmentVariable("JEV_API_KEY"), File.Exists(file) ? File.ReadAllText(file) : null);
    }

    // The variable wins when it holds a key; a blank variable or a blank file counts as no key.
    public static string? KeyFrom(string? variable, string? fileText)
    {
        if (!string.IsNullOrWhiteSpace(variable)) return variable.Trim();
        return string.IsNullOrWhiteSpace(fileText) ? null : fileText.Trim();
    }
}
