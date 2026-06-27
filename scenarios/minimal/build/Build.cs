using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// Tier 0 smoke scenario. Proves the released Fallout.Common package restores from the
// GitHub Packages feed, its source generator runs (the Target backing code below only
// compiles if the generator produced it), and a generated tool wrapper (DotNet) drives a
// real build. The default target is Compile.
class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    AbsolutePath SampleProject => RootDirectory / "src" / "Sample" / "Sample.csproj";

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(SampleProject));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(SampleProject)
                .EnableNoRestore());
        });
}
