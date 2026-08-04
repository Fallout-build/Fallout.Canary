# scenario: tool-manifest

Upgrades the `fallout` dotnet tool from the version a consumer is pinned to, to the newest published
one, the way a consumer repo does.

Unlike the other scenarios this one does not build a `_build.csproj`. It exercises **package
identity** rather than package contents, and — like [`upgrade`](../upgrade) — it tests a *transition*
rather than a snapshot:

1. The committed manifest pins `fallout.globaltool` at **10.3.49**, the last GA of the previous
   production line. `dotnet tool restore` must resolve it: if that pin is ever unlisted out from
   under a consumer, their restore breaks with no warning.
2. `dotnet tool update fallout.globaltool --prerelease` performs the upgrade hop. `--prerelease`
   takes the newest published version of **this** package id, and cannot cross to a different one.
   That is precisely why the rename stranded consumers, and it is the regression this catches.
3. The resolved version must have actually moved off the baseline — a no-op upgrade means the hop
   silently resolved nothing.
4. `dotnet tool restore` + `dotnet tool list --local` confirm the `fallout` command is wired up.

The manifest is rewritten during the run, so the committed baseline pin stays put and every run
re-tests the same hop. Bump it once per production line.

This scenario asserts package **identity and resolution**, not execution. `dotnet fallout` needs a
build project and a `.fallout` root marker before any of its commands exit 0, so proving the tool
*runs* needs a full consumer build — that is the separate tier-1 scenario the design doc sketches.

It reads from **nuget.org only** (see `nuget.config` here), not the GitHub Packages `-preview` feed
the other scenarios use, because the failure being guarded against is a public-consumer failure.

## Why this exists

[Fallout-build/Fallout#575](https://github.com/Fallout-build/Fallout/issues/575): the tool package id
changed from `Fallout.GlobalTool` to `Fallout.GlobalTools`, the old id was left published and not
deprecated, and consumers pinning it silently stopped receiving updates. `rollForward` does not help,
because it resolves a version within one package id and cannot cross to a different one.

Nothing in the canary covered the tool's install path at the time, so the rename shipped unnoticed —
even though the root `nuget.config` already had a source mapping for `fallout.globaltools`.

**The rename was since reverted**: `Fallout.GlobalTool` (singular) is the shipping id, and
`Fallout.GlobalTools` is unlisted and deprecated with `Fallout.GlobalTool` as its alternate. This
scenario originally floated the plural id and went red on 2026-07-29 when that id was unlisted —
correct behaviour for the check it was making, against the wrong id.
