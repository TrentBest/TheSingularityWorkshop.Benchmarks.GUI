using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

/// <summary>
/// Measures WPF host attachment, layout, update, routed-command dispatch, and teardown.
/// Runs synchronously on the process's STA thread; it does not show a visible window
/// and therefore makes no paint, input-device, or user-perceived latency claim.
/// </summary>
internal static class WpfLifecycleMeasurements
{
    private const int SamplesPerCase = 15;
    private static readonly Size HostSize = new(700, 500);

    public static int Run()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException("WPF lifecycle measurements require an STA thread.");

        _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var outputDirectory = Path.Combine("results", "wpf");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "wpf-lifecycle.csv");
        var rows = new List<string>
        {
            "implementation,sections,sample,construction_ms,attach_layout_ms,update_ms,command_dispatch_ms,teardown_ms,dispatched_commands"
        };

        foreach (var sections in new[] { 2, 6, 18 })
        {
            foreach (var implementation in new[] { "native", "gui" })
            {
                // Exclude initial JIT/type initialization from the reported sample set.
                for (var warmup = 0; warmup < 3; warmup++)
                    _ = Measure(implementation == "gui", sections);
            }

            // Alternate paired order to reduce a fixed native-first or GUI-first bias.
            for (var sample = 1; sample <= SamplesPerCase; sample++)
            {
                var order = sample % 2 == 1
                    ? new[] { "native", "gui" }
                    : new[] { "gui", "native" };

                foreach (var implementation in order)
                {
                    var result = Measure(implementation == "gui", sections);
                    rows.Add(string.Join(",",
                        implementation,
                        sections.ToString(CultureInfo.InvariantCulture),
                        sample.ToString(CultureInfo.InvariantCulture),
                        Format(result.Construction),
                        Format(result.AttachAndLayout),
                        Format(result.Update),
                        Format(result.CommandDispatch),
                        Format(result.Teardown),
                        result.DispatchedCommands.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        File.WriteAllText(outputPath, string.Join(Environment.NewLine, rows) + Environment.NewLine, new UTF8Encoding(false));
        Console.WriteLine($"WPF lifecycle samples: {outputPath}");
        Console.WriteLine($"Warmups per implementation/workload: 3; measured samples: {SamplesPerCase}; window visible: false; thread: {Thread.CurrentThread.GetApartmentState()}");
        Console.WriteLine(string.Join(Environment.NewLine, rows));
        return 0;
    }

    private static Measurement Measure(bool useGui, int sections)
    {
        FrameworkElement? content = null;
        Window? window = null;
        var dispatched = 0;

        var construction = Time(() =>
        {
            content = useGui
                ? WpfGuiRenderer.Render(CreateSemanticTree(sections), _ => dispatched++)
                : BuildNativeTree(sections, () => dispatched++);
        });

        var attachAndLayout = Time(() =>
        {
            window = new Window
            {
                Width = HostSize.Width,
                Height = HostSize.Height,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
                Content = content
            };

            content!.Measure(HostSize);
            content.Arrange(new Rect(new Point(0, 0), HostSize));
            content.UpdateLayout();
            window.Dispatcher.Invoke(static () => { }, DispatcherPriority.ApplicationIdle);
        });

        var update = Time(() =>
        {
            var heading = FindById<TextBlock>(content!, "dashboardHeading");
            heading.Text = "Workshop dashboard updated";
            content!.UpdateLayout();
        });

        var commandDispatch = Time(() =>
        {
            var button = FindById<Button>(content!, "section0Save");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });

        if (dispatched != 1)
            throw new InvalidOperationException($"Expected exactly one command dispatch, observed {dispatched}.");

        var teardown = Time(() =>
        {
            window!.Content = null;
            window.Close();
            content = null;
            window = null;
        });

        return new Measurement(construction, attachAndLayout, update, commandDispatch, teardown, dispatched);
    }

    private static TimeSpan Time(Action action)
    {
        var start = Stopwatch.GetTimestamp();
        action();
        return Stopwatch.GetElapsedTime(start);
    }

    private static string Format(TimeSpan value) =>
        value.TotalMilliseconds.ToString("F6", CultureInfo.InvariantCulture);

    private static T FindById<T>(DependencyObject root, string id) where T : FrameworkElement
    {
        if (root is T candidate && string.Equals(GetId(candidate), id, StringComparison.Ordinal))
            return candidate;

        // Logical traversal works before a Window is shown and its visual templates are realized.
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject dependency)
                continue;

            var found = FindById<T>(dependency, id);
            if (found is not null)
                return found;
        }

        throw new InvalidOperationException($"Could not find WPF element '{id}'.");
    }

    private static string? GetId(FrameworkElement element) =>
        element.GetValue(WpfGuiIdentity.NodeIdProperty) as string ?? element.Name;

    private static GuiNode CreateSemanticTree(int sections)
    {
        var children = new List<GuiNode>
        {
            new("Text", "dashboardHeading", "Workshop dashboard")
        };

        for (var section = 0; section < sections; section++)
        {
            var controls = new List<GuiNode>
            {
                new("Text", $"section{section}Heading", $"Section {section + 1}"),
                new("Button", $"section{section}Save", "Save",
                    properties: new Dictionary<string, string> { ["onClickCommand"] = "save" }),
                new("Warning", $"section{section}Notice", "Changes are saved locally.")
            };
            children.Add(new GuiNode("Panel", $"section{section}", children: controls));
        }

        return new GuiNode("Column", "dashboard", children: children);
    }

    private static FrameworkElement BuildNativeTree(int sections, Action dispatch)
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
            var button = new Button { Name = $"section{section}Save", Content = "Save" };
            button.Click += (_, _) => dispatch();
            sectionContent.Children.Add(button);
            sectionContent.Children.Add(new Border
            {
                Name = $"section{section}Notice",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x1B, 0x08)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xA0, 0x30)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = "Changes are saved locally.",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xD0, 0x90)),
                    TextWrapping = TextWrapping.Wrap
                }
            });
            root.Children.Add(new Border { Name = $"section{section}", Child = sectionContent });
        }

        return root;
    }

    private sealed record Measurement(
        TimeSpan Construction,
        TimeSpan AttachAndLayout,
        TimeSpan Update,
        TimeSpan CommandDispatch,
        TimeSpan Teardown,
        int DispatchedCommands);
}
