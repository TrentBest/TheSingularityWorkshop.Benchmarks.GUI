using BenchmarkDotNet.Attributes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        var root = new StackPanel { Name = "dashboard", Orientation = Orientation.Vertical };
        root.Children.Add(new TextBlock { Name = "dashboardHeading", Text = "Workshop dashboard", TextWrapping = TextWrapping.Wrap });

        for (var section = 0; section < sections; section++)
        {
            var sectionContent = new StackPanel { Orientation = Orientation.Vertical };
            sectionContent.Children.Add(new TextBlock
            {
                Name = $"section{section}Heading",
                Text = $"Section {section + 1}",
                TextWrapping = TextWrapping.Wrap
            });
            sectionContent.Children.Add(new Button
            {
                Name = $"section{section}Save",
                Content = "Save"
            });
            sectionContent.Children.Add(new Border
            {
                Name = $"section{section}Notice",
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x1B, 0x08)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xA0, 0x30)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = "Changes are saved locally.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD0, 0x90)),
                    TextWrapping = TextWrapping.Wrap
                }
            });

            root.Children.Add(new Border
            {
                Name = $"section{section}",
                Child = sectionContent
            });
        }

        return root;
    }
}
