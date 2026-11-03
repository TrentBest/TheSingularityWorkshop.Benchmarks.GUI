using BenchmarkDotNet.Running;

namespace TheSingularityWorkshop.Benchmarks.GUI.BlazorBench;

internal static class Program
{
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
