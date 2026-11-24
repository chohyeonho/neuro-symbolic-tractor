using Nst.Application;
using Nst.Tests.Judging;

namespace Nst.Tests;

// The base checks that the structure holds and that each verification tool turns on.
// Later pull requests hold nst to its rules.
public class BaseTests
{
    [Fact]
    public void Nst_runs_with_no_arguments()
    {
        var r = NstProcess.Run();
        Assert.Equal(0, r.ExitCode);
        Assert.Equal([ActionTable.Header + " No actions yet."], r.OutLines);
    }

    [Fact]
    public void Nst_refuses_an_action_it_does_not_have()
    {
        var r = NstProcess.Run("nothing");
        Assert.Equal(2, r.ExitCode);
        Assert.Empty(r.OutLines);
        Assert.Contains("no action 'nothing'", r.Err);
    }

    [Fact]
    public async Task Judge_answers_one_question()
    {
        var judge = Judges.Current();
        Assert.SkipWhen(judge is null, "No judge key (JEV_API_KEY or ~/.config/typesafe/api_key).");
        var answers = await judge!.Ask("nst prints at most 120 lines per call.",
            new Dictionary<string, string> { ["states_a_limit"] = "Does the text state a limit on how much is printed?" });
        Assert.InRange(answers["states_a_limit"], 0.0, 1.0);
    }
}
