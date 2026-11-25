# neuro-symbolic-tractor
Farm your knowledge. Let AI agents harvest only what's ripe.

`nst` is the tractor: the one program an AI session is allowed to run. Everything goes through it,
and every nst action caps the number of lines it prints per call (not the length of a line).

## Consumer and producer

nst knows two roles.

### Consumer: rides the machine

An AI session doing a project's work. nst is the tractor it rides: everything it does goes through the tractor,
and nst is the only program the session can run.
How it is held to nst is up to whoever launches it; nst does not know.
Like a brain's working memory, its context holds only so much at once, so every nst action caps the number of lines it prints per call.
A line's length is not capped: a file of one very long line comes out whole.

### Producer: builds the machine

Whoever builds nst. It changes nst only through pull requests to this repository.

### Between them

Requests go one way, from the consumer to the producer. The consumer says what it needs;
the producer decides how nst meets that need, and a reviewed pull request decides whether it goes in.
The consumer never changes nst itself.

nst is built for the consumer first. Nothing in nst serves the producer yet.

## Reference environment

nst is built and checked against one fixed setup. Set it up first: [environment](docs/setup/environment.md).

| Part | Role | Pinned |
|---|---|---|
| Claude Code | the CLI that runs each AI session | 2.1.331 |
| Orca | opens and keeps the terminals the sessions run in | 1.4.248 |
| Jev | the outside judge model the tests call | `jev-1.14.0` |
| .NET | builds and runs nst | SDK 10.0.404 or a later 10.0 (`global.json`) |
| Lean | checks the proofs | 4.36.0 (`lean/lean-toolchain`) |

Lean and C# are fixed for good: they do not differ from one person to the next.
The others differ, so each has a slot to swap it in (see Structure).

## Structure

nst is split into layers. Each layer only uses the layers inside it, and `ArchitectureTests` checks that.

| Layer | Project | Holds |
|---|---|---|
| Domain | `src/Nst.Domain` | rules and values, with no input or output. Lean models this layer |
| Application | `src/Nst.Application` | the actions (`IAction`), the table that runs them, and the ports (interfaces) they reach the outside through |
| Infrastructure | `src/Nst.Infrastructure` | adapters that fill the ports with real files and programs |
| Composition | `src/nst` | the program itself: builds every action with its adapters, runs one, prints the result |

Slots for what can be swapped:

- **Outside programs nst calls** (a window, a game engine, a site builder) are reached through a port.
  Each comes with its adapter in the pull request that first needs it, so swapping the program touches one adapter.
- **The judge** is a slot in the tests (`tests/Nst.Tests/Judging`): `IJudge`, filled by `JevJudge`.
  Another judging model fits by adding one adapter and changing `Judges`.
- **The session program** (Claude Code today) is not called by nst; it calls nst.
  It is swapped in how sessions are launched, and nst's code does not change.

## Build

```sh
dotnet build nst.slnx -c Release                   # every project
dotnet test --project tests/Nst.Tests -c Release   # runtime checks: runs the built nst (nst.dll) as a process
cd lean
lake build                                        # the proofs (Lean 4, version pinned in lean/lean-toolchain)
cd ..
```

Each command is on its own line, so the block runs the same in Windows PowerShell, which has no `&&`,
and the last line brings you back to the repository root, where the commands below run.

## Install

```sh
dotnet publish src/nst -c Release -o out/nst      # one native program: out/nst/nst.exe on Windows, out/nst/nst elsewhere
```

Put the `out/nst` folder on the `PATH` of the session that rides nst, then launch the session as the
[environment](docs/setup/environment.md) shows. Running `nst` from any project folder then prints its actions.
Publishing to one native program needs a native compiler toolchain besides the .NET SDK
([Native AOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#prerequisites)):

| Platform | Also install |
| --- | --- |
| Windows | the Visual Studio C++ build tools, with the Installer folder on `PATH` (see the [environment](docs/setup/environment.md)) |
| Ubuntu | `sudo apt-get install clang` |
| Fedora, RHEL | `sudo dnf install clang` |
| macOS | the latest Command Line Tools for Xcode (`xcode-select --install`) |

## Verification

Three tools check nst, each where the others cannot reach.

- **Lean** proves a rule on a model of the code that keeps it (`lean/`).
- **Runtime tests** run nst as a process and hold it to the same rule (`tests/Nst.Tests`).
  They run the managed build that `dotnet build` makes. The consumer rides the native program that `dotnet publish` makes,
  so CI also publishes that program and runs it.
  They also run layers in process and check the layer directions.
- **A judge** (Jev, a paid outside model) judges what code cannot decide. It only gives a signal; a test decides.
  Its key comes from `JEV_API_KEY` (in CI, a repository secret) and is never printed. Without a key its tests are skipped.
