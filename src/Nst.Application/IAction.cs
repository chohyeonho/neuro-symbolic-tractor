namespace Nst.Application;

// One thing a session can ask nst to do: `nst <Name> <args>`.
// An action reaches files or other programs only through a port (an interface in this layer).
public interface IAction
{
    string Name { get; }

    // One line shown in the action list, starting with how to call it.
    string Usage { get; }

    ActionResult Run(IReadOnlyList<string> args);
}
