# scenario: upgrade

Takes one consumer's build source **unchanged** across a Fallout version bump, and asserts they can
reach green.

Every other scenario floats to the newest package and asks "does this build?". This one is the only
scenario that tests a **transition**: it pins the version a real consumer is sitting on, bumps it,
and checks what happens. A regression here is not "the new release is broken" — it is "the new
release is fine, and existing users cannot get to it".

## The hop

| | Version | Where it comes from |
|---|---|---|
| Baseline | `10.3.49` | `FalloutBaselineVersion` in [`build/_build.csproj`](build/_build.csproj) — pinned, because it is history |
| Target | newest listed on nuget.org | resolved per-run by the workflow |

nuget.org only ever carries the production line (`main`'s `-preview` builds go to GitHub Packages and
never here), so "newest listed on nuget.org" *is* the current line. That makes the hop
self-maintaining: it becomes `10.3.49 → 10.4.0` the moment 10.4.0 GA ships, with no edit here.

Bump `FalloutBaselineVersion` once per production line — when 10.4 becomes the previous line, the
baseline becomes the last 10.4 GA.

This scenario reads **nuget.org only** (see [`nuget.config`](nuget.config)), so unlike `minimal` and
`transition-shims` it needs no `FALLOUT_PACKAGES_TOKEN`. A consumer upgrading between released lines
restores from nuget.org; routing this hop through the `-preview` feed would test an upgrade nobody
performs.

## What the three stages mean

1. **Build at the baseline.** Must be green. If this fails the scenario itself is broken and says
   nothing about the upgrade.
2. **Bump the version, change nothing else.** This is the entire edit a consumer makes. Green means
   the upgrade is clean.
3. **Fall back to `fallout-migrate`.** Runs only when stage 2 broke. Green means the upgrade is
   rough but survivable; red means it is *impossible* and the scenario fails.

Stage 2 failing is deliberately **not** a scenario failure. "Breaks on a bare bump, fixed by one
`fallout-migrate` run" is a supported upgrade path. What must never happen is a consumer with no path
at all — so that is what fails the build. Which path was taken is written to the run summary, because
a consumer needing `fallout-migrate` means the release notes owe them an upgrade note.

## Why `Build.cs` looks like that

[`build/Build.cs`](build/Build.cs) is deliberately written the way a 10.3-era consumer writes it, and
is **not** modernised. It leans on the surface that actually moved:

```csharp
using Fallout.Common.ProjectModel;   // moved to Fallout.Solutions on 10.4

[Solution] readonly Solution Solution;
Project SampleProject => Solution.GetProject("Sample");
```

`Solution`, `SolutionAttribute`, `Project`, and the `GetProject` extension all lived in
`Fallout.Common.ProjectModel` on the 10.3 line. The transition shim in `Fallout.Common` re-exports
`Solution` and `SolutionAttribute` — but, by design, not `Project` and not the extension methods. So
a build that merely *declares* a solution keeps compiling, and a build that *navigates the project
graph* breaks. That distinction is the whole defect, and it is why the shim's existence was not
enough.

Keeping the `using` directive and the `Project` usage is what makes this scenario test anything.
Don't "fix" them.

## Why this exists

[Fallout-build/Fallout#619](https://github.com/Fallout-build/Fallout/issues/619): bumping
`Fallout.Common` 10.3.49 → 10.4.0-rc.5 fails with a single `CS0246: The type or namespace name
'Project' could not be found`. Reproduced on FluentAssertions, a real consumer, by changing only the
version. `fallout-migrate` fixes it completely — but nothing led a consumer to run it, because its
own description is "migrate a NUKE consumer repo to Fallout" and a maintainer bumping a Fallout
version has no reason to think it applies to them.

Related: [#575](https://github.com/Fallout-build/Fallout/issues/575) covers the same class of failure
for the global tool's package id, and is guarded by [`tool-manifest`](../tool-manifest).
[#618](https://github.com/Fallout-build/Fallout/issues/618) is why the baseline build emits `NU1903`
warnings: `Fallout.Common` 10.3.49 depends on a vulnerable
`System.Security.Cryptography.Xml` 10.0.6, patched on the 10.4 line. They are warnings here, so the
baseline stays green — a consumer with `TreatWarningsAsErrors` sees them as errors instead.

## Running locally

```bash
cd scenarios/upgrade

# Stage 1 — the consumer today.
dotnet run --project build/_build.csproj

# Stage 2 — the bump. `*-*` is the csproj default for the target.
rm -rf build/obj build/bin
dotnet run --project build/_build.csproj -p:FalloutVersion='*-*'
```

If stage 2 breaks, install the migrator **outside** the scenario and pass the path explicitly —
`fallout-migrate` resolves its own root by walking up from the working directory and will rewrite
whatever it finds ([#617](https://github.com/Fallout-build/Fallout/issues/617)):

```bash
dotnet tool install Fallout.Migrate --version 10.4.0-rc.5 --tool-path /tmp/migrate
/tmp/migrate/fallout-migrate "$PWD"
git diff        # see what it rewrote, then revert before committing
```
