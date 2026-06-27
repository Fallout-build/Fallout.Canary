using Nuke.Common;

// Tier 2 compatibility scenario. Authored against the LEGACY Nuke.* surface exactly as a
// pre-rebrand NUKE consumer would have written it. It compiles and runs only if the
// Nuke.Common transition shim package (NukeBuild + the Target/Execute shims) and the
// source generator that flows through it both work from the packaged feed.
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Verify);

    Target Verify => _ => _
        .Executes(() =>
        {
            System.Console.WriteLine($"Transition shim build OK. RootDirectory = {RootDirectory}");
        });
}
