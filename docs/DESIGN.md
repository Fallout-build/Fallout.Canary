# fallout.canary — design

## Why this exists

Fallout's own test suite (`tests/`, xUnit + Verify snapshots) tests the framework **internals in isolation** — project references, generators running in-tree, etc. It cannot see the failures that only appear once the code is **packed and consumed as a NuGet package**:

| Gap | Unit tests | Canary |
|-----|:---------:|:------:|
| `Pack` produces an installable, restorable package (right deps/targets/`buildTransitive` assets) | ✗ | ✓ |
| Source generators run against the **packaged analyzer**, not a project reference | ✗ | ✓ |
| `dotnet fallout` global tool installs and runs from the packed tool | ✗ | ✓ |
| `Nuke.*` shims let a mid-migration consumer compile + run | partial | ✓ |
| Build-graph / schema / IDE tooling consume real released output | ✗ | ✓ |

This is acceptance/canary testing of the **artifact**, not the source.

## Decisions (locked 2026-06-27)

- **One repo, scenarios as subfolders** (`scenarios/<name>/`) — single CI matrix, whole picture in one place. Heavy real-world scenarios may graduate to their own repos later.
- **Triggers:** `repository_dispatch` (`fallout-preview-published`) from Fallout's release on every `-preview` publish **+** nightly schedule **+** manual dispatch.
- **Issues filed here**, deduped per scenario, auto-closed on green. (If a regression is confirmed real, cross-link an issue on `Fallout-build/Fallout` by hand — escalation stays manual for now.)
- **Channel = GitHub Packages `-preview`** (`https://nuget.pkg.github.com/Fallout-build/index.json`). The `-preview` feed is what we want early warning on. GA/`nuget.org` validation can be added as a separate trigger/matrix axis when a `release/YYYY` line is cut.
- **Float, don't pin:** `Version="2026.1.*-*"`. Floating is what makes it a canary — every new release gets exercised automatically. Reproducibility comes from the resolved version printed in the run log (and captured in any failure issue), not from a lockfile.

## Auth

The canary repo's default `GITHUB_TOKEN` **cannot** read packages owned by `Fallout-build/Fallout` unless those packages are explicitly granted to this repo or made org-internal. So CI authenticates to the feed with a **PAT** (classic, `read:packages`) stored as the repo secret **`FALLOUT_PACKAGES_TOKEN`**, wired into `nuget.config` via env-var expansion.

The same applies to the `repository_dispatch` link: Fallout's `preview.yml` must POST a dispatch to this repo using a token with `repo`/dispatch scope on `fallout.canary`. Until that one-line step is added to `preview.yml`, the nightly schedule keeps the canary functional. See "Wiring the dispatch" below.

## Wiring the dispatch (follow-up, needs a PR on Fallout)

Add to `Fallout/.github/workflows/preview.yml` after the publish step (touches the production workflow → normal PR, `target/2026` label):

```yaml
      - name: 'Notify: fallout.canary'
        if: success()
        run: |
          gh api repos/Fallout-build/fallout.canary/dispatches \
            -f event_type=fallout-preview-published
        env:
          GH_TOKEN: ${{ secrets.CANARY_DISPATCH_TOKEN }}
```

## Failure → issue contract

- Title: `Canary failure: <scenario>` (stable per scenario → dedupe key).
- Label: `canary-failure`.
- First failure opens the issue with the run URL + resolved package version; subsequent consecutive failures add a comment instead of opening duplicates.
- A green run for that scenario closes the open issue with a comment.

## Roadmap of scenarios

- **Tier 0 — `minimal`** *(built)* — package restore + generator + one tool wrapper.
- **Tier 2 — `transition-shims`** *(built)* — `class Build : NukeBuild` against the `Nuke.Common` shim.
- **Tier 1 — `global-tool`** — `dotnet tool install fallout.globaltools --prerelease`, run a target via `dotnet fallout`.
- **Tier 1 — `tool-wrappers`** — exercise several generated tool wrappers (dotnet, git, …).
- **Tier 3 — `large-graph`** — many interdependent targets, conditional/skipped targets, matrix parameters, fan-out.
- **Tier 3 — `ide-tooling`** — `build-graph.json` export validated against `build.schema.json`.
