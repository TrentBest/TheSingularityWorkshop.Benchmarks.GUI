# WPF workload definition — v1

## Scope

The executable suite now has two explicitly separate layers:

1. **STA host/lifecycle sample** — semantic/native tree construction, assignment to an equivalent WPF Window host, explicit Measure/Arrange/UpdateLayout plus an ApplicationIdle dispatcher drain, a content update, a synthetic routed Button.Click dispatch, and teardown. The lifecycle harness runs five warmups followed by 30 measured paired samples for each implementation at each workload size and writes raw per-phase timing and current-thread allocation counts to results/wpf/wpf-lifecycle.csv.

The former WPF BenchmarkDotNet construction case was retired: BenchmarkDotNet executes its workload on an MTA worker, while WPF control construction requires STA. Its all-NA result was a harness failure, not performance evidence. The current WPF project intentionally exposes only the STA-capable lifecycle runner; do not reintroduce an MTA WPF benchmark.

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

The STA lifecycle harness owns a Window, assigns the content, performs explicit measure/arrange/layout, updates the heading, dispatches a routed click, and closes/clears the host. Both native and GUI trees use the same workload content and host dimensions.

## Result naming and boundaries

Lifecycle CSV contains independent phase timing and allocation columns for construction, attach/layout, update, synthetic command dispatch, and teardown. These are independent phase measurements, not a single end-to-end score. Allocation values use GC.GetAllocatedBytesForCurrentThread around the phase; they are per-thread managed allocation deltas, not total process memory. The lifecycle run uses an unshown window and synthetic routed event; it must not be described as visible UI rendering, physical input latency, or user-perceived responsiveness.

## Running the lifecycle harness

From the repository root, after checking out the GUI repository's `development` branch into the expected sibling folder:

```powershell
dotnet run --project src/WpfBench/TheSingularityWorkshop.Benchmarks.GUI.WpfBench.csproj --configuration Release -- --lifecycle
```

The command requires Windows and an STA-capable process. It writes `results/wpf/wpf-lifecycle.csv`; the workflow uploads this file as an artifact. The CSV contains raw per-sample phase timings. Summaries should be calculated from the retained samples, with the five warmups excluded.

## Diagnostic pipeline decomposition

The STA runner also writes \`results/wpf/wpf-pipeline-diagnostics.csv\`. This diagnostic isolates the exact private production methods used by the public GUI renderer:

- **Validation only** invokes \`WpfGuiRenderer.ValidateTree(GuiNode)\`.
- **Materialization only** invokes \`WpfGuiRenderer.RenderValidated(GuiNode, Action<string>?)\` and retains the resulting FrameworkElement tree beyond the timed region.
- The semantic GuiNode tree is constructed before either measurement, so its construction cost is excluded from both diagnostic phases. The existing lifecycle \`construction\` measurement remains unchanged and continues to represent the complete public \`WpfGuiRenderer.Render\` call, including semantic-tree construction, validation, and materialization.

The private production methods are bound to typed delegates once before measurement; reflection is not timed, and no validator or renderer implementation is copied into the benchmark. A missing or changed private method signature fails the diagnostic rather than silently measuring a substitute. Each workload size receives five warmups and 30 measured samples, with validation/materialization order alternated per sample. Timing and current-thread managed allocation deltas are recorded independently. These GUI-only diagnostics explain the internal pipeline; they are not a native-versus-GUI comparison and must not be added together as an end-to-end score.
