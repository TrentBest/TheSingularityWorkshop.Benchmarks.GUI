# GUI Adapter Benchmark Findings — 2026-11-05 Hosted Run

> **Evidence status:** verified measurement artifact from one hosted run. This is stronger and more complete than the initial November 4 capture because the browser CSV is present and validated, but it is still one run—not a stable cross-machine baseline or proof of statistical significance.

- Workflow: [Measure GUI adapter baselines — run 41987386471](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/runs/41987386471)
- Artifact: [gui-adapter-measurements-41987386471](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/runs/41987386471/artifacts/13396297669)
- Benchmark repository branch: `development`
- Tracking: [Issue #1 — Establish user-facing WPF and Blazor performance baselines](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/1)
- Follow-up profiling: [Issue #3 — Profile GUI adapter construction and render-tree allocation hotspots](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/3)

## 1. Executive summary

This run compares equivalent GUI-adapter and native workloads at three sizes. The main findings are:

1. **Blazor large render-tree result is promising:** at 18 sections, GUI.Blazor's measured .NET render-tree construction mean was **9.278 μs**, compared with **10.821 μs** for native Blazor. That is approximately **14.3% less time** for the GUI case in this benchmark.
2. **That is a workload-specific result, not a universal speed claim.** At 2 sections GUI was about 2% faster; at 6 sections it was about 6% slower; at 18 sections it was about 14% faster. The timing relationship changes with workload size.
3. **The GUI render-tree case allocates more memory at every size.** At 18 sections, it allocated 18.44 KB versus 14.56 KB native—about 27% more—even while taking less time in this run.
4. **WPF is close to native in most measured lifecycle phases.** Construction is consistently somewhat slower and allocates more; attach/layout, update, command dispatch, and teardown are broadly near parity.
5. **The browser comparison artifact is now present and validated.** It contains 60 rows: 30 native and 30 GUI samples. Browser medians favor the GUI static page in this run, but a large native first-sample cold-start outlier makes a single-run headline inappropriate.

The useful engineering story is therefore not simply “the adapter is faster” or “the adapter is slower.” It is that adapter overhead depends on workload size and lifecycle phase. The larger Blazor render-tree case demonstrates that the abstraction can be competitive—and can outperform the native comparison in a measured path—while memory allocation remains an optimization target.

## 2. How to read the numbers

- **Absolute time** answers how long the measured operation took (for example, 9.278 microseconds).
- **GUI/native ratio** is the GUI result divided by the native result. A ratio below 1.0 means GUI took less time; above 1.0 means GUI took more time.
- **Allocation** reports managed memory allocated during the measured operation. Lower time does not imply lower allocation, and allocation is not the same as retained heap size or total process working set.
- **Median** is the middle observation in a sample set and is less sensitive to extreme samples than a mean.
- **Mean** is the arithmetic average. The Blazor render-tree figures below are BenchmarkDotNet means, not medians.
- **One hosted run** is an observation. Repeat independent runs and examine distributions before treating a difference as stable or statistically meaningful.

Microseconds (μs) are one-millionth of a second. These values describe the isolated operation named by the benchmark, not the duration of a whole application frame or a user's complete interaction.

## 3. Blazor render-tree construction

These BenchmarkDotNet measurements capture .NET-side render-tree construction. They do **not** measure browser parsing, layout, paint, interactive startup, or event handling.

| Sections | Native mean | GUI mean | GUI / native | Native allocated | GUI allocated | Allocation ratio |
|---:|---:|---:|---:|---:|---:|---:|
| 2 | 1.316 μs | 1.285 μs | 0.98x | 1.99 KB | 3.08 KB | 1.55x |
| 6 | 3.101 μs | 3.275 μs | 1.06x | 6.68 KB | 8.69 KB | 1.30x |
| 18 | 10.821 μs | 9.278 μs | 0.86x | 14.56 KB | 18.44 KB | 1.27x |

### The 18-section result in plain language

For the largest tested render-tree workload, the native implementation averaged 10.821 μs and the GUI adapter averaged 9.278 μs. The difference is 1.543 μs per measured operation. Relative to the native time, the GUI case used approximately 14.3% less time:

`(10.821 - 9.278) / 10.821 × 100 ≈ 14.3%`

This tells us that the adapter's render-tree construction path is not inherently slower than writing the equivalent native component tree. In this larger workload, it was faster in the captured run. That is an encouraging result and a goal worth pursuing.

There is a second half to the result: GUI allocated 18.44 KB compared with 14.56 KB native, approximately 26.6% more. So the adapter completed the measured operation sooner while allocating more managed memory. We should investigate where those allocations come from rather than hiding the trade-off or assuming the faster time makes memory irrelevant.

The smaller workloads reinforce why results must be reported by workload size: the GUI case was slightly faster at 2 sections, slightly slower at 6, and faster at 18. The data suggests scaling behavior worth investigating; it does not yet explain the cause.

## 4. WPF lifecycle comparison

The WPF harness uses a dedicated STA lifecycle runner. It records construction, Window content attachment/layout, update, synthetic routed-command dispatch, and teardown. The Window is hidden; this is not a visible-paint or physical-input test.

The following are **per-phase medians in milliseconds**, with native shown before GUI.WPF:

| Sections | Phase | Native (ms) | GUI.WPF (ms) | GUI / native |
|---:|---|---:|---:|---:|
| 2 | Construction | 0.4163 | 0.4437 | 1.066x |
| 2 | Attach + layout | 2.8516 | 2.8597 | 1.003x |
| 2 | Update | 0.3988 | 0.4052 | 1.016x |
| 2 | Synthetic command | 0.0156 | 0.0160 | 1.026x |
| 2 | Teardown | 0.1448 | 0.1449 | 1.001x |
| 6 | Construction | 0.9248 | 0.9724 | 1.051x |
| 6 | Attach + layout | 6.5412 | 6.5152 | 0.996x |
| 6 | Update | 0.4071 | 0.3952 | 0.971x |
| 6 | Synthetic command | 0.0149 | 0.0151 | 1.013x |
| 6 | Teardown | 0.2668 | 0.2709 | 1.015x |
| 18 | Construction | 2.4681 | 2.5738 | 1.043x |
| 18 | Attach + layout | 18.2166 | 18.2962 | 1.004x |
| 18 | Update | 0.4072 | 0.4323 | 1.062x |
| 18 | Synthetic command | 0.0144 | 0.0146 | 1.014x |
| 18 | Teardown | 0.7638 | 0.7556 | 0.989x |

### WPF construction allocations

| Sections | Native allocated | GUI.WPF allocated | GUI / native |
|---:|---:|---:|---:|
| 2 | 23,448 B | 28,032 B | 1.196x |
| 6 | 66,008 B | 77,032 B | 1.167x |
| 18 | 193,392 B | 223,056 B | 1.153x |

Construction is approximately 4–7% slower and allocates approximately 15–20% more in the GUI case. Most other lifecycle phases are close to native in these samples. These results point to construction and its allocation footprint as a practical profiling target; they do not indicate a large penalty across the entire measured WPF lifecycle.

## 5. Browser-hosted Blazor static-render comparison

The browser CSV is present in this run's artifact and contains exactly 60 data rows: 30 native and 30 GUI. The harness uses Playwright/Chromium, alternates paired sample order, and records navigation timing plus layout dimensions.

| Metric | Native median | GUI median |
|---|---:|---:|
| Time to first byte (TTFB) | 2.3 ms | 2.4 ms |
| DOMContentLoaded | 20.2 ms | 11.8 ms |
| Load | 20.25 ms | 12.45 ms |

The native first sample is a substantial cold-start outlier (TTFB 541.9 ms, DOMContentLoaded 553.0 ms, load 553.9 ms). Excluding that one sample, native DOMContentLoaded median is 20.0 ms and load median is 20.0 ms; the GUI medians remain 11.8 ms and 12.4 ms respectively. The raw sample must remain part of the evidence, and the exclusion is shown only to make the cold-start effect transparent—not to silently discard an inconvenient observation.

This harness measures loopback HTTP, server-side static HTML rendering, browser parsing, and forced style/layout. It does not measure interactive Blazor startup, event callback latency, physical paint completion, or user-input-to-visible-response. Treat these values as an initial static-page comparison, not a complete browser UX benchmark.

## 6. Environment and evidence quality

- BenchmarkDotNet 0.14.0
- .NET SDK 10.0.403 installed; benchmark runtime .NET 8.0.33
- x64 RyuJIT AVX2
- Windows 11 build 10.0.26100.33674 / Hyper-V reported by BenchmarkDotNet
- Captured host manifest: Windows Server 2025 Datacenter, build 26100, x64, 4 logical CPUs, approximately 16 GB RAM
- Processor model was not reported, limiting hardware-to-hardware comparisons.

The workflow completed successfully and uploaded:
- BenchmarkDotNet Blazor report files and log;
- WPF lifecycle CSV with 180 per-sample records;
- browser CSV with 60 per-sample records;
- host, .NET, and capture-time environment manifests.

The workflow's browser validation checks the expected file and row counts before upload. A successful workflow and intact CSVs establish artifact completeness, not repeatability or statistical significance.

## 7. What this run establishes—and what it does not

### Supported by this artifact

- In this run, the 18-section GUI.Blazor render-tree construction mean was about 14.3% lower than native.
- GUI.Blazor allocated more managed memory than native at all three render-tree workload sizes.
- WPF GUI construction was modestly slower and allocated more, while most other measured lifecycle phases were close to native.
- The static browser comparison artifact is present, with the expected 30 samples per implementation.
- The measured relationship varies by workload and by phase.

### Not established

- That GUI.Blazor is generally 14% faster, or that it is faster for all application shapes.
- That the observed differences are statistically significant or will repeat on another run or machine.
- Interactive Blazor event latency, full application startup, or user-perceived responsiveness.
- Visible WPF paint or physical-input latency.
- Retained heap size, garbage-collection impact under sustained application use, or total process memory.
- Performance across other hardware, runtimes, browsers, or workload compositions.

## 8. Next experiments

1. Repeat the hosted measurement on several independent runs and retain each raw artifact. Compare distributions and run-to-run variance.
2. Under [Issue #3](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/3), isolate Blazor validation traversal, semantic-tree traversal, fragment/delegate creation, and RenderTreeBuilder frame allocation.
3. Profile WPF construction to separate validation/container costs from native control and styling allocations.
4. Preserve duplicate-ID and maximum-depth validation guarantees during any optimization.
5. Extend browser testing to interaction only when the adapter has a defined, tested event contract.

## 9. LLM / engineering handoff

**Current interpretation:** the adapter is competitive with native implementations in the measured workloads. The standout result is the 18-section Blazor render-tree case (9.278 μs GUI versus 10.821 μs native, approximately 14.3% less time), paired with a higher allocation cost (18.44 KB versus 14.56 KB). This is an encouraging workload-specific observation, not a universal performance claim.

**Recommended next question:** what work and allocations are incurred by GUI.Blazor's validation and rendering pipeline, and can we reduce allocation without losing correctness or the favorable large-workload timing?

When reusing this report in another LLM session, preserve the benchmark boundary and caveats above. Do not convert the single-run result into a general product claim.

---

**Governance:** this report is documentation and measurement analysis on `development`. It does not authorize NuGet publication, branch merging, or changes to `master`.
