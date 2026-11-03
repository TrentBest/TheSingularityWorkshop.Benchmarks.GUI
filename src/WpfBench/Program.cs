using BenchmarkDotNet.Running;

namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--lifecycle", StringComparison.OrdinalIgnoreCase))
            return WpfLifecycleMeasurements.Run();

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
    }
}
