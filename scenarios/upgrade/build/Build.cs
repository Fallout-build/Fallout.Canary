using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.ProjectModel;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// Tier 1 upgrade scenario. This file is deliberately written the way a consumer on the
// PREVIOUS production line writes it, and is not modernised: the point is to take exactly
// that source across a version bump and see whether it still compiles.
//
// The surface it leans on is the surface that moved. `Solution`, `SolutionAttribute`,
// `Project`, and the `GetProject` extension all lived in `Fallout.Common.ProjectModel` on
// the 10.3 line and moved to `Fallout.Solutions` on 10.4. The transition shim in
// Fallout.Common re-exports Solution and SolutionAttribute but — by design — not Project
// and not the extensions, so a build that navigates the project graph breaks on the bump
// and `fallout-migrate` is what fixes it.
//
// See Fallout-build/Fallout#619. Keep this using-directive and the Project usage below:
// removing either is what makes the scenario stop testing anything.
class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Solution] readonly Solution Solution;

    Project SampleProject => Solution.GetProject("Sample");

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

            // Proves the graph really was navigated, not just named.
            AbsolutePath assembly = SampleProject.Directory / "bin" / "Debug" / "net10.0" / "Sample.dll";
            Assert.FileExists(assembly);
        });
}
