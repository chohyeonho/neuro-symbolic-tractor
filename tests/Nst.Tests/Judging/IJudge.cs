namespace Nst.Tests.Judging;

// The judge slot: an outside model that answers yes/no questions about a text with a probability of yes.
// It judges what code cannot decide, and only gives a signal; a test decides what to do with it.
// Any model that can answer this way fits the slot. JevJudge is the pinned one.
public interface IJudge
{
    // Name and version of the model behind this judge, for test output.
    string Model { get; }

    // questions: name -> instruction. Returns name -> probability of yes, from 0 to 1.
    Task<IReadOnlyDictionary<string, double>> Ask(string text, IReadOnlyDictionary<string, string> questions);
}
