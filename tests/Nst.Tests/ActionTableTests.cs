using Nst.Application;

namespace Nst.Tests;

// The action table, run in process with a stand-in action: a new action plugs in without changing the table.
public class ActionTableTests
{
    sealed class Echo : IAction
    {
        public string Name => "echo";
        public string Usage => "nst echo <words>   print the words back";
        public ActionResult Run(IReadOnlyList<string> args) => ActionResult.Done([string.Join(' ', args)]);
    }

    [Fact]
    public void Lists_each_action_with_no_arguments()
    {
        var r = new ActionTable([new Echo()]).Run([]);
        Assert.Equal(0, r.ExitCode);
        Assert.Equal([ActionTable.Header, "  nst echo <words>   print the words back"], r.Out);
    }

    [Fact]
    public void Runs_the_named_action_with_the_rest_of_the_arguments()
    {
        var r = new ActionTable([new Echo()]).Run(["echo", "a", "b"]);
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(["a b"], r.Out);
    }

    [Fact]
    public void Refuses_two_actions_with_one_name()
    {
        Assert.Throws<ArgumentException>(() => new ActionTable([new Echo(), new Echo()]));
    }
}
