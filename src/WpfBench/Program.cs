using BenchmarkDotNet.Running;

namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
