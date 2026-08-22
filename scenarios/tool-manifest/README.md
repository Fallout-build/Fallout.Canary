# scenario: tool-manifest

Restores the `fallout` dotnet tool from a local manifest and runs it, the way a consumer repo does.

Unlike the other scenarios this one does not build a `_build.csproj`. It exercises **package
identity** rather than package contents:

1. `dotnet tool update Fallout.GlobalTool --prerelease` floats the manifest to the newest published
   tool. This step fails if the published tool package id ever changes again, which is the regression
   this scenario exists to catch.
2. `dotnet tool restore` resolves the pin.
3. `dotnet tool list --local` confirms the `fallout` command is wired up.

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

Nothing in the canary covered the tool's install path at the time, so the rename shipped unnoticed.

**The rename was then reverted.** `Fallout.Cli.csproj` sets `<PackageId>Fallout.GlobalTool</PackageId>`
again, so the singular id is canonical and `Fallout.GlobalTools` is the retired one. The plural id got a
single nuget.org release (`10.4.0-rc.4`, since unlisted) before the revert. This scenario tracks the
singular id and reports on the retired ones.
