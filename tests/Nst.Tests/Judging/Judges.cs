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

    // From JEV_API_KEY (CI uses a repository secret), else ~/.config/typesafe/api_key.
    static string? JevKey()
    {
        var env = Environment.GetEnvironmentVariable("JEV_API_KEY");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "typesafe", "api_key");
        return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
    }
}
