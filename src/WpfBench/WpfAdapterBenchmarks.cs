using BenchmarkDotNet.Attributes;
using System.Windows;
using System.Windows.Controls;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

/// <summary>
/// Isolates warm in-process tree construction and manifestation.
/// It deliberately does not claim to measure window startup, layout, paint, or interaction.
/// </summary>
[MemoryDiagnoser]
public class WpfAdapterBenchmarks
{
    [Params(2, 6, 18)]
    public int Sections { get; set; }

    private GuiNode _semanticTree = null!;

    [GlobalSetup]
    public void Setup() => _semanticTree = GuiWorkloads.CreateSemanticTree(Sections);

    [Benchmark(Baseline = true, Description = "Native WPF control-tree construction")]
    public FrameworkElement NativeWpfConstruction() => BuildNativeTree(Sections);

    [Benchmark(Description = "GUI.WPF semantic manifestation")]
    public FrameworkElement GuiWpfManifestation() => WpfGuiRenderer.Render(_semanticTree);

    private static FrameworkElement BuildNativeTree(int sections)
    {
        var root = new StackPanel { Name = "dashboard", Orientation = Orientation.Vertical, Margin = new Thickness(8), IsEnabled = true };
        root.Children.Add(new TextBlock { Name = "dashboardheading", Text = "Workshop dashboard" });

        for (var section = 0; section < sections; section++)
        {
            var panel = new Grid { Name = $"section{section}" };
            panel.Children.Add(new TextBlock { Name = $"section{section}heading", Text = $"Section {section + 1}" });
            panel.Children.Add(new TextBox
            {
                Name = $"section{section}name",
                Text = $"Value {section + 1}",
                Width = 240,
                Margin = new Thickness(4)
            });
            panel.Children.Add(new Button { Name = $"section{section}save", Content = "Save", IsEnabled = true, Margin = new Thickness(4) });
            panel.Children.Add(new Border
            {
                Name = $"section{section}notice",
                Child = new TextBlock { Text = "Changes are saved locally.", TextWrapping = TextWrapping.Wrap },
                Padding = new Thickness(8)
            });
            root.Children.Add(panel);
        }

        return root;
    }
}
