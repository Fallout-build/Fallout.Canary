# fallout.canary

Early-warning **canary** for [Fallout](https://github.com/Fallout-build/Fallout). It consumes Fallout's published GitHub Packages `-preview` feed **exactly like a real user would** — references the packages, runs real builds — to catch regressions that the framework's own unit tests can't:

- packaging gaps (`Pack` produces a package that doesn't actually restore/build for a consumer);
- source generators that work against a project reference but not against the **packaged analyzer**;
- the `dotnet fallout` global tool failing to install or run;
- the `Nuke.*` → `Fallout.*` transition shims failing to compile/run for a mid-migration consumer.

If a scenario's build breaks, the canary **opens (or updates) an issue here**, labelled [`canary-failure`](https://github.com/Fallout-build/fallout.canary/labels/canary-failure), and **auto-closes it** when the scenario goes green again.

> This repo contains **no product code** — only consumer projects that exercise the released artifacts. The design rationale lives in [`docs/DESIGN.md`](docs/DESIGN.md).

## How it works

```
release on Fallout (-preview → GitHub Packages)
        │  repository_dispatch: fallout-preview-published
        ▼
   canary CI  ──┬── scenario: minimal           ─┐
   (also runs   ├── scenario: transition-shims    ├─ each = a real consumer build
    nightly +   └── …                             ─┘   floated to the latest -preview
    on demand)
        │
        ├─ all green → close any open canary-failure issues
        └─ a scenario fails → open/update its canary-failure issue
```

- **Triggers:** `repository_dispatch` from Fallout's release on every `-preview` publish (fast feedback) **+** a nightly schedule (safety net) **+** manual `workflow_dispatch`.
- **Floating:** scenarios reference `2026.1.*-*`, so every run pulls the newest `-preview`. The resolved version is printed in each run's log.
- **Isolation:** one CI job per scenario (`fail-fast: false`) — one broken scenario doesn't mask the others.

## Scenario tiers

| Tier | Scenario | Asserts |
|------|----------|---------|
| 0 — smoke | [`minimal`](scenarios/minimal) | `Fallout.Common` restores from the feed; a build with `Restore`/`Compile` targets compiles (source generator ran) and drives the `dotnet` tool wrapper. |
| 2 — compat | [`transition-shims`](scenarios/transition-shims) | A build authored against the legacy `Nuke.*` namespaces (`class Build : NukeBuild`) compiles and runs against the `Nuke.Common` shim package. |

Tiers 1 (tool wrappers, global tool) and 3 (large target graph, multi-solution, IDE tooling) are sketched in the design doc and land incrementally.

## Add a scenario

1. Create `scenarios/<name>/` with a Fallout consumer build at `scenarios/<name>/build/_build.csproj` and a `Build.cs` whose default target is what you want exercised.
2. Reference the released package(s) with a floated version: `Version="2026.1.*-*"`.
3. Add `<name>` to the `scenario` matrix in [`.github/workflows/canary.yml`](.github/workflows/canary.yml).

A scenario "passes" when its build exits 0. For stronger guarantees, add assertions on the build output inside the build's targets (fail the target if an expected artifact/member is missing).

## Running locally

```bash
# A PAT with read:packages on the Fallout-build org:
export FALLOUT_PACKAGES_TOKEN=ghp_xxx
dotnet run --project scenarios/minimal/build/_build.csproj
```
