# WPF workload definition — v1

## Scope

The executable suite now has two explicitly separate layers:

1. **Warm construction** — WpfGuiRenderer.Render(GuiNode) versus direct native WPF construction, measured by BenchmarkDotNet.
2. **STA host/lifecycle sample** — semantic/native tree construction, assignment to an equivalent WPF Window host, explicit Measure/Arrange/UpdateLayout plus an ApplicationIdle dispatcher drain, a content update, a synthetic routed Button.Click dispatch, and teardown. The lifecycle harness runs three warmups followed by 15 measured samples for each implementation at each workload size and writes raw per-sample CSV to results/wpf/wpf-lifecycle.csv.

The lifecycle harness does not call Window.Show and does not use physical input. It measures host content attachment and explicit layout work, not visible presentation, compositor paint, real user input, or end-to-end application startup. The synthetic click measures WPF routed-command dispatch only.

## Scenario matrix

| Parameter | Small | Medium | Large |
|---|---:|---:|---:|
| Sections | 2 | 6 | 18 |
| Root nodes | 1 | 1 | 1 |
| Section panels | 2 | 6 | 18 |
| Controls per section | 3 | 3 | 3 |
| Total semantic nodes | 10 | 26 | 74 |
| Maximum tree depth | 3 | 3 | 3 |

Each section contains a heading, a save button, and a warning message. The root is a vertical Column containing the dashboard heading and section panels.

## Equivalence notes

The semantic and native paths create the same visible content and corresponding WPF element types for the verified development-branch subset: StackPanel, Border/StackPanel section panels, TextBlock, Button, and styled Border/TextBlock warnings. WPF's semantic renderer currently maps Warning to a Border containing a TextBlock.

The BenchmarkDotNet construction harness returns the root element and does not attach it to a Window. The separate STA lifecycle harness owns a Window, assigns the content, performs explicit measure/arrange/layout, updates the heading, dispatches a routed click, and closes/clears the host. Both native and GUI trees use the same workload content and host dimensions.

## Result naming and boundaries

BenchmarkDotNet methods remain NativeWpfConstruction and GuiWpfManifestation. Lifecycle CSV fields are construction_ms, attach_layout_ms, update_ms, command_dispatch_ms, and teardown_ms. These are independent phase timings, not a single end-to-end score. The lifecycle run uses an unshown window and synthetic routed event; it must not be described as visible UI rendering, physical input latency, or user-perceived responsiveness.

## Running the lifecycle harness

From the repository root, after checking out the GUI repository's `development` branch into the expected sibling folder:

```powershell
dotnet run --project src/WpfBench/TheSingularityWorkshop.Benchmarks.GUI.WpfBench.csproj --configuration Release -- --lifecycle
```

The command requires Windows and an STA-capable process. It writes `results/wpf/wpf-lifecycle.csv`; the workflow uploads this file as an artifact. The CSV contains raw per-sample phase timings. Summaries should be calculated from the retained samples, with the three warmups excluded.
