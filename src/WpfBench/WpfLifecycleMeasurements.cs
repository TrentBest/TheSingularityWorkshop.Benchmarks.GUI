using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
    private const int SamplesPerCase = 30;
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
            "implementation,sections,sample,construction_ms,construction_alloc_bytes,attach_layout_ms,attach_layout_alloc_bytes,update_ms,update_alloc_bytes,command_dispatch_ms,command_dispatch_alloc_bytes,teardown_ms,teardown_alloc_bytes,dispatched_commands"
        };

        foreach (var sections in new[] { 2, 6, 18 })
        {
            foreach (var implementation in new[] { "native", "gui" })
            {
                // Exclude initial JIT/type initialization from the reported sample set.
                for (var warmup = 0; warmup < 5; warmup++)
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
                        Format(result.Construction.Elapsed),
                        result.Construction.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                        Format(result.AttachAndLayout.Elapsed),
                        result.AttachAndLayout.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                        Format(result.Update.Elapsed),
                        result.Update.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                        Format(result.CommandDispatch.Elapsed),
                        result.CommandDispatch.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                        Format(result.Teardown.Elapsed),
                        result.Teardown.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                        result.DispatchedCommands.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        File.WriteAllText(outputPath, string.Join(Environment.NewLine, rows) + Environment.NewLine, new UTF8Encoding(false));
        RunPipelineDiagnostics(outputDirectory);

        Console.WriteLine($"WPF lifecycle samples: {outputPath}");
        Console.WriteLine($"Warmups per implementation/workload: 5; measured samples: {SamplesPerCase}; window visible: false; thread: {Thread.CurrentThread.GetApartmentState()}");
        Console.WriteLine(string.Join(Environment.NewLine, rows));
        return 0;
    }


    private static void RunPipelineDiagnostics(string outputDirectory)
    {
        var renderer = typeof(WpfGuiRenderer);
        var validateMethod = renderer.GetMethod(
            "ValidateTree",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("WpfGuiRenderer.ValidateTree was not found.");
        var materializeMethod = renderer.GetMethod(
            "RenderValidated",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("WpfGuiRenderer.RenderValidated was not found.");

        // Bind exact production methods once, outside all measured regions.
        Action<GuiNode> validate = validateMethod.CreateDelegate<Action<GuiNode>>();
        Func<GuiNode, Action<string>?, FrameworkElement> materialize =
            materializeMethod.CreateDelegate<Func<GuiNode, Action<string>?, FrameworkElement>>();

        var rows = new List<string>
        {
            "implementation,sections,sample,validation_ms,validation_alloc_bytes,materialization_ms,materialization_alloc_bytes"
        };

        foreach (var sections in new[] { 2, 6, 18 })
        {
            for (var warmup = 0; warmup < 5; warmup++)
            {
                var warmupTree = CreateSemanticTree(sections);
                validate(warmupTree);
                _ = materialize(warmupTree, static _ => { });
            }

            for (var sample = 1; sample <= SamplesPerCase; sample++)
            {
                // Tree construction is deliberately outside both diagnostic phases.
                var tree = CreateSemanticTree(sections);
                PhaseMeasurement validation;
                PhaseMeasurement materialization;
                FrameworkElement? rendered = null;

                if (sample % 2 == 1)
                {
                    validation = MeasurePhase(() => validate(tree));
                    materialization = MeasurePhase(() => rendered = materialize(tree, static _ => { }));
                }
                else
                {
                    materialization = MeasurePhase(() => rendered = materialize(tree, static _ => { }));
                    validation = MeasurePhase(() => validate(tree));
                }

                rows.Add(string.Join(",",
                    "gui",
                    sections.ToString(CultureInfo.InvariantCulture),
                    sample.ToString(CultureInfo.InvariantCulture),
                    Format(validation.Elapsed),
                    validation.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                    Format(materialization.Elapsed),
                    materialization.AllocatedBytes.ToString(CultureInfo.InvariantCulture)));

                GC.KeepAlive(rendered);
            }
        }

        var outputPath = Path.Combine(outputDirectory, "wpf-pipeline-diagnostics.csv");
        File.WriteAllText(outputPath, string.Join(Environment.NewLine, rows) + Environment.NewLine, new UTF8Encoding(false));
        Console.WriteLine("WPF pipeline diagnostics: " + outputPath);
        Console.WriteLine("Diagnostic warmups per phase/workload: 5; measured samples: " + SamplesPerCase + "; thread: " + Thread.CurrentThread.GetApartmentState());
        Console.WriteLine(string.Join(Environment.NewLine, rows));
    }

    private static Measurement Measure(bool useGui, int sections)
    {
        FrameworkElement? content = null;
        Window? window = null;
        var dispatched = 0;

        var construction = MeasurePhase(() =>
        {
            content = useGui
                ? WpfGuiRenderer.Render(CreateSemanticTree(sections), _ => dispatched++)
                : BuildNativeTree(sections, () => dispatched++);
        });

        var attachAndLayout = MeasurePhase(() =>
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

        var update = MeasurePhase(() =>
        {
            var heading = GetHeading(content!);
            heading.Text = "Workshop dashboard updated";
            content!.UpdateLayout();
        });

        var commandDispatch = MeasurePhase(() =>
        {
            var button = GetFirstSaveButton(content!);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });

        if (dispatched != 1)
            throw new InvalidOperationException($"Expected exactly one command dispatch, observed {dispatched}.");

        var teardown = MeasurePhase(() =>
        {
            window!.Content = null;
            window.Close();
            content = null;
            window = null;
        });

        return new Measurement(construction, attachAndLayout, update, commandDispatch, teardown, dispatched);
    }

    private static PhaseMeasurement MeasurePhase(Action action)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        action();
        var elapsed = Stopwatch.GetElapsedTime(start);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return new PhaseMeasurement(elapsed, allocatedBytes);
    }

    private static string Format(TimeSpan value) =>
        value.TotalMilliseconds.ToString("F6", CultureInfo.InvariantCulture);

    private static StackPanel GetRootPanel(FrameworkElement content) =>
        content as StackPanel
        ?? throw new InvalidOperationException("The matched WPF workload root must be a StackPanel.");

    private static TextBlock GetHeading(FrameworkElement content)
    {
        var root = GetRootPanel(content);
        return root.Children.Count > 0 && root.Children[0] is TextBlock heading
            ? heading
            : throw new InvalidOperationException("The matched WPF workload is missing its dashboard heading.");
    }

    private static Button GetFirstSaveButton(FrameworkElement content)
    {
        var root = GetRootPanel(content);
        if (root.Children.Count < 2
            || root.Children[1] is not Border section
            || section.Child is not StackPanel sectionContent
            || sectionContent.Children.Count < 2
            || sectionContent.Children[1] is not Button button)
        {
            throw new InvalidOperationException("The matched WPF workload is missing the first section's Save button.");
        }

        return button;
    }

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
        PhaseMeasurement Construction,
        PhaseMeasurement AttachAndLayout,
        PhaseMeasurement Update,
        PhaseMeasurement CommandDispatch,
        PhaseMeasurement Teardown,
        int DispatchedCommands);

    private sealed record PhaseMeasurement(TimeSpan Elapsed, long AllocatedBytes);
}
