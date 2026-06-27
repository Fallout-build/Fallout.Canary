using Nuke.Common;     // shimmed: NukeBuild (a renamed type, hand-bridged to Fallout.Common.FalloutBuild)
using Fallout.Common;  // canonical: the Target delegate is intentionally NOT shimmed — delegates can't be
                       // subclassed cross-assembly, so TransitionShimGenerator skips them (SHIM002). A
                       // mid-migration consumer keeps Target on the canonical name and migrates the rest.

// Tier 2 compatibility scenario. Models a realistic *partially migrated* consumer: the
// build base still uses the legacy `NukeBuild` shim, while target declarations use the
// canonical `Fallout.Common.Target`. Compiles + runs only if the Nuke.Common shim package
// bridges NukeBuild correctly and the canonical source generator flows through the feed.
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Verify);

    Target Verify => _ => _
        .Executes(() =>
        {
            System.Console.WriteLine($"Transition shim build OK. RootDirectory = {RootDirectory}");
        });
}
