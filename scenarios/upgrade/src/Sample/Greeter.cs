namespace Sample;

// Trivial production code so the scenario's build has something real to compile, and so the
// solution has a project for `Solution.GetProject("Sample")` to find.
public static class Greeter
{
    public static string Greet(string name) => $"Hello from the Fallout upgrade canary, {name}!";
}
