# neuro-symbolic-tractor
Farm your knowledge. Let AI agents harvest only what's ripe.

`nst` is the tractor: the one program an AI session is allowed to run. Everything goes through it,
and every nst action caps what it prints.

## Reference environment

nst is built and checked against one fixed setup. Set it up first: [environment](docs/setup/environment.md).

| Part | Role | Pinned |
|---|---|---|
| Claude Code | the CLI that runs each AI session | 2.1.331 |
| Orca | opens and keeps the terminals the sessions run in | 1.4.248 |
| Jev | the outside judge model the tests call | `jev-1.14.0` |
| .NET | builds and runs nst | SDK 10.0.403 or a later 10.0 (`global.json`) |
| Lean | checks the proofs | 4.36.0 (`lean/lean-toolchain`) |

Lean and C# are fixed for good: they do not differ from one person to the next.
