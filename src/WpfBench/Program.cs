namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--lifecycle", StringComparison.OrdinalIgnoreCase))
            return WpfLifecycleMeasurements.Run();

        Console.Error.WriteLine("WPF measurements require the STA lifecycle harness. Run with --lifecycle.");
        return 2;
    }
}
