namespace Nst.Application;

// Picks the action a call names and runs it. Adding an action never changes this class:
// the composition root hands it every action there is.
public sealed class ActionTable
{
    public const string Header = "nst: your only tool.";

    readonly IReadOnlyList<IAction> _actions;

    public ActionTable(IEnumerable<IAction> actions)
    {
        _actions = actions.ToList();
        var twice = _actions.GroupBy(a => a.Name).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
            throw new ArgumentException($"Two actions share the name '{twice.Key}'.", nameof(actions));
    }

    public IReadOnlyList<IAction> Actions => _actions;

    public ActionResult Run(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return ActionResult.Done(List());
        var action = _actions.FirstOrDefault(a => a.Name == args[0]);
        if (action is null)
            return ActionResult.Misused($"nst: no action '{args[0]}'. Run nst with no arguments for the list.");
        return action.Run(args.Skip(1).ToList());
    }

    List<string> List()
    {
        if (_actions.Count == 0) return [Header + " No actions yet."];
        return [Header, .. _actions.Select(a => "  " + a.Usage)];
    }
}
