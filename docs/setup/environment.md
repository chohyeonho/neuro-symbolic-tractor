# Environment

nst is built and checked against one fixed setup. Set it up before anything else.
A setup not listed here is not promised to work. Other setups are supported later by extending from this one.

When a pinned version is raised, change this document, the README table and the pin file in the same PR.

## Pinned parts

| Part | Role | Pinned | Where the pin lives |
| --- | --- | --- | --- |
| Claude Code | the CLI that runs each AI session | 2.1.331 | this document |
| Orca | opens and keeps the terminals the sessions run in | 1.4.248 | this document |
| Jev | the outside judge model the tests call | `jev-1.14.0` | this document; the judge sends it as its model |
| .NET | builds and runs nst | SDK 10.0.404 or a later 10.0 SDK | `global.json` |
| Lean | checks the proofs | 4.36.0 | `lean/lean-toolchain` |

Lean and C# are fixed for good: they do not differ from one person to the next.
The others can differ from one person or project to the next, so nst gives each of them a slot to swap it in.

## Setting up

### .NET

Install the .NET 10 SDK. `global.json` asks for 10.0.404 and lets a later 10.0 SDK stand in for it.
10.0.404 is the SDK of .NET 10.0.15 (November 2026), whose fixes include ones to the .NET runtime itself
(CVE-2026-109475, denial of service; CVE-2026-105544, elevation of privilege).
A native program carries the runtime inside it, so earlier SDKs are refused.
Take each later patch release as it comes out, and raise the pin with it.

nst is published as one native program. On Windows, `dotnet publish` also needs the Visual Studio C++ build tools
(the "Desktop development with C++" workload), and the Visual Studio Installer folder on `PATH` so the build can find them.
`dotnet build` and `dotnet test` do not need them.

### Lean

Install elan, the Lean version manager. elan reads `lean/lean-toolchain` and fetches that Lean the first time it is used there.
Nothing else needs to be installed for Lean.

### Claude Code

Install Claude Code 2.1.331 with the native installer, naming the version so the installer does not pick a release channel:

| Shell | Command |
| --- | --- |
| macOS, Linux, WSL | `curl -fsSL https://claude.ai/install.sh \| bash -s 2.1.331` |
| Windows PowerShell | `& ([scriptblock]::Create((irm https://claude.ai/install.ps1))) 2.1.331` |

Native installs update themselves in the background, and a later Claude Code can change the permission behavior this guide relies on.
The consumer settings file sets `DISABLE_AUTOUPDATER` to `1`, so a consumer session does not update the install.
The install is shared, so set the same `env` entry in `~/.claude/settings.json` for every other session on the machine.
`claude --version` shows the version in use.
To move to a later version, raise the pin as the top of this document says, and repeat the restriction check below on the new version first.

On native Windows, also install [Git for Windows](https://git-scm.com/downloads/win).
The consumer launch below keeps only the Bash tool, and Claude Code needs Git Bash to provide it.

What a session is told to do lives in the global `CLAUDE.md` (`~/.claude/CLAUDE.md`), not in this repository.

A session that rides nst (the consumer) is launched like this:

```
claude --restricted --tools Bash --settings "<path to consumer-settings.json>" --permission-mode dontAsk --strict-mcp-config --disable-slash-commands
```

- `--restricted` ignores the user, project and local settings files, so permissions given elsewhere do not leak in.
  The settings file named with `--settings` applies, and so do managed settings, which an organization's administrator sets for the machine.
  This guide assumes a machine with no managed settings. Managed rules that allow more commands would also apply to the consumer.
- `--tools Bash` leaves the shell as the only built-in tool.
- The consumer settings file allows `nst` and `nst *` and denies the file tools.
  Claude Code still runs a built-in set of read-only commands, such as `pwd` and `echo`, without any allow rule, and that set is not listed in full.
  So the file also runs a check before every Bash call that refuses the call unless it is `nst` followed by plain arguments:
  no quotes, no shell operators such as `&&`, `;`, `|` or `>`, and no `$`, `~` or globs.
  An argument also cannot hold a space, `(`, `)`, `[`, `]`, `{`, `}`, `\`, `!` or `#`,
  so the consumer cannot name a file whose name holds any of these characters,
  such as `[id].tsx`, `app/(auth)/page.tsx` or a name with a space. Separate folders with `/`.
  If the check cannot run, the call is refused too (`onFailure: block`).
  Use [consumer-settings.json](consumer-settings.json), the file this setup was checked with.
- `--permission-mode dontAsk` denies anything that is not allowed, without asking.
- `--strict-mcp-config` and `--disable-slash-commands` keep MCP servers and skills out.
- `nst` must be on the session's `PATH`.

The settings file belongs to whoever launches the session. nst does not read it.

After launching, check the restriction once: ask the session to run `nst`, then `pwd`, `echo hi`, `ls`, `cat README.md`, `git status` and `nst && ls`, one call each.
`nst` runs, and every other call is refused with "Only nst may run in this session."
This setup was checked that way with Claude Code 2.1.331 on Windows.

### Orca

Install [Orca 1.4.248](https://github.com/stablyai/orca/releases/tag/v1.4.248). Orca opens the terminals the sessions run in.
The `orca` command ships with the app: register it under Settings → General → Orca CLI, so `orca --version` works in a terminal.
The roles do not need Orca: a terminal for each session is enough. Orca is only the pinned way to open them.

### Jev

Get a Jev API key from TypeSafe.
Set it as `JEV_API_KEY` in the environment to run the judged tests locally, and as the repository secret `JEV_API_KEY` for CI.
Never write the key into a file in the repository.

## Checking the setup

| Run | Expect |
| --- | --- |
| `claude --version` | `2.1.331 (Claude Code)` |
| `orca --version` | `1.4.248` |
| `dotnet --version` (at the repository root) | `10.0.404` or a later `10.0` SDK |
| `lake --version` (in `lean/`) | Lean version `4.36.0` |

Sources: [Claude Code CLI](https://code.claude.com/docs/en/cli-reference), [Claude Code permissions](https://code.claude.com/docs/en/permissions), [Claude Code read-only commands](https://code.claude.com/docs/en/permissions#read-only-commands), [Claude Code hooks](https://code.claude.com/docs/en/hooks), [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json), [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/), [elan](https://github.com/leanprover/elan), [Claude Code setup](https://code.claude.com/docs/en/setup), [Orca CLI](https://orca.dev/docs/cli/overview), [.NET 10.0.15 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.15/10.0.15.md), [Jev API](https://docs.typesafe.ai/api/system-one), [Jev models](https://docs.typesafe.ai/models).
