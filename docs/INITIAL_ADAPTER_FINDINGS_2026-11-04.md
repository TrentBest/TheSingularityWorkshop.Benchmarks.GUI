# GUI Adapter Benchmark Findings — Initial Hosted Run

> **Evidence status:** preliminary, single hosted run. These measurements are useful for review and for choosing the next experiments; they are not yet stable performance baselines and do not justify a universal faster/slower claim.

- Workflow: [Measure GUI adapter baselines — run 41757495313](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/runs/41757495313)
- Artifact: [gui-adapter-measurements-41757495313](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/runs/41757495313/artifacts/13289150889)
- Benchmark repository revision: `b8768e785aaa0745f2bde3c1ddd83c8d97abc582` (`development`)
- GUI source: checked out from `TheSingularityWorkshop.GUI/development`
- Tracking: [Issue #1 — Establish user-facing WPF and Blazor performance baselines](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/1)

## 1. Executive summary

The first full hosted measurement run gives us a useful initial picture:

1. **WPF:** GUI.WPF is close to the native WPF implementation for attach/layout, update, synthetic command dispatch, and teardown in these workloads. Construction is modestly more expensive in both time and managed allocation.
2. **Blazor render-tree/component work:** timings are mixed by workload size. The GUI case is slightly slower for the small workload, effectively equal for medium, and faster for large in this run. The GUI case allocates more memory at every size.
3. **Blazor browser comparison:** the harness executed 30 paired samples per implementation and reported writing its CSV, but that CSV is absent from the uploaded artifact. No browser timing results are published in this report until the raw data is recoverable and verified.
4. **Confidence:** one hosted run is a starting observation only. Repeat runs, variance analysis, and artifact integrity checks are required before calling any result a baseline.

## 2. WPF host lifecycle

The WPF lifecycle harness ran on the Windows hosted runner using an STA-capable process. It performed five warmups and 30 paired samples per implementation/workload. The Window was not shown; layout and dispatcher work were explicitly driven by the harness. Command dispatch was synthetic.

The table reports **median milliseconds per phase**. The ratio is GUI.WPF divided by native WPF; values above 1.0 mean GUI.WPF took longer in this run.

| Sections | Phase | Native (ms) | GUI.WPF (ms) | GUI / native |
|---:|---|---:|---:|---:|
| 2 | Construction | 0.4678 | 0.5044 | 1.078x |
| 2 | Attach + layout | 3.0766 | 3.1594 | 1.027x |
| 2 | Update | 0.4274 | 0.4404 | 1.031x |
| 2 | Synthetic command | 0.0192 | 0.0195 | 1.018x |
| 2 | Teardown | 0.1656 | 0.1638 | 0.989x |
| 6 | Construction | 0.9068 | 0.9712 | 1.071x |
| 6 | Attach + layout | 6.4769 | 6.5447 | 1.010x |
| 6 | Update | 0.4305 | 0.4316 | 1.003x |
| 6 | Synthetic command | 0.0213 | 0.0214 | 1.005x |
| 6 | Teardown | 0.2749 | 0.2732 | 0.994x |
| 18 | Construction | 2.5050 | 2.6212 | 1.046x |
| 18 | Attach + layout | 18.0179 | 18.2088 | 1.011x |
| 18 | Update | 0.4983 | 0.4998 | 1.003x |
| 18 | Synthetic command | 0.0211 | 0.0229 | 1.083x |
| 18 | Teardown | 0.8099 | 0.7936 | 0.980x |

### WPF construction allocations

Managed bytes allocated during construction:

| Sections | Native (B) | GUI.WPF (B) | GUI / native |
|---:|---:|---:|---:|
| 2 | 23,448 | 28,032 | 1.196x |
| 6 | 66,008 | 77,032 | 1.167x |
| 18 | 193,392 | 223,056 | 1.153x |

The update and synthetic-command allocation measurements matched between the two implementations in the captured run. Attach/layout and teardown allocation differences were small relative to construction. Consult the raw WPF CSV in the workflow artifact for the per-sample records.

### WPF interpretation

- Attach/layout medians were approximately 1.0–2.7% higher for GUI.WPF across the three sizes.
- Update medians were approximately 0.3–3.1% higher.
- Construction medians were approximately 4.6–7.8% higher, with a larger managed allocation footprint.
- Teardown was approximately 0.6–2.0% lower for GUI.WPF in this run.
- The large-workload synthetic-command ratio is noisier than the other command results and should not be over-interpreted from one run.

This is encouraging evidence that the adapter's lifecycle work is not introducing a large measured penalty in these particular phases, but construction and allocation are clear areas to investigate. It is not evidence about visible desktop paint, physical input, or user-perceived latency: the Window was hidden and the command was synthetic.

## 3. Blazor render-tree/component work

These are BenchmarkDotNet means for .NET-side render-tree construction. They are **not browser rendering measurements**.

| Sections | Native mean | GUI mean | GUI / native | Native allocated | GUI allocated | Allocation ratio |
|---:|---:|---:|---:|---:|---:|---:|
| 2 | 1.224 µs | 1.319 µs | 1.08x | 1.99 KB | 3.08 KB | 1.55x |
| 6 | 3.638 µs | 3.650 µs | 1.00x | 6.68 KB | 8.69 KB | 1.30x |
| 18 | 9.479 µs | 8.351 µs | 0.88x | 14.56 KB | 18.44 KB | 1.27x |

### Blazor interpretation

- Small: GUI render-tree construction was about 8% slower.
- Medium: timings were effectively equal at the displayed precision.
- Large: GUI was about 12% faster by mean in this run.
- GUI allocated approximately 27–55% more managed memory across these sizes.

The timing direction changes with workload size, so it would be misleading to summarize this as either “GUI.Blazor is faster” or “GUI.Blazor is slower.” The additional allocations are consistent across all three cases and deserve investigation. These results need repeated runs and distribution review before drawing stronger conclusions.

The run's BenchmarkDotNet environment header reported BenchmarkDotNet 0.14.0, Windows 11 build 10.0.26100.33674 (Hyper-V), .NET SDK 10.0.403 installed, and .NET 8.0.33 x64 RyuJIT AVX2. The processor was reported as unknown in the benchmark header; the workflow also captured host and .NET environment manifests in the artifact.

## 4. Blazor browser comparison — execution confirmed, data not retained

The browser harness was configured to compare the GUI and native static-rendered pages using headless Playwright Chromium, a 1280×800 viewport, device scale factor 1, and 30 paired samples per implementation. The order alternated between native-first and GUI-first. It recorded navigation response start, DOMContentLoaded, load, and dashboard layout dimensions.

The workflow log for run 41757495313 contains the harness completion line:

`Browser comparison samples written: results\\browser\\blazor-browser.csv (30 per implementation; paired alternating order).`

However, the uploaded ZIP artifact contains no `results/browser/blazor-browser.csv`. The artifact upload log reports eight files uploaded; its contents include the WPF CSV, environment files, and BenchmarkDotNet reports, but not the browser CSV.

**Conclusion:** execution is confirmed by the job log, but the raw browser data is not available for independent analysis. Therefore this report intentionally publishes no browser timing values and makes no browser-performance conclusion. The precise reason the generated file was not present in the artifact is not yet established.

The browser test's scope is also narrower than interactive Blazor UX: it measures loopback HTTP, server-side static HTML rendering, browser parsing, and forced style/layout calculation. It does not measure interactive Blazor startup, adapter event latency, physical paint, or user input-to-visible-completion.

## 5. What this run does and does not establish

### Supported observations

- WPF GUI/native lifecycle medians are close for most measured phases in the selected workloads.
- WPF construction is somewhat slower and allocates more in the GUI case.
- Blazor render-tree timing is workload-size dependent.
- Blazor render-tree construction allocates more in the GUI case at every measured size.
- The browser harness ran, but its CSV was not retained in the artifact.

### Not established

- A stable performance baseline or a statistically significant difference.
- A general performance ranking of GUI versus native implementations.
- Visible WPF paint or physical input responsiveness.
- Interactive Blazor startup or event-handling performance.
- Browser GUI/native performance, because the raw CSV is missing.
- Performance on other machines, runtime versions, workloads, or application shapes.

## 6. Follow-up work

1. **Repair artifact integrity:** make the browser harness resolve its output path explicitly, verify the file exists immediately after execution, parse it, and fail the workflow if the expected 60 data rows (30 per implementation) are absent. Also print the absolute output path and file size.
2. **Repeat the full measurement run:** capture multiple independent hosted runs, preserve each raw artifact, and compare medians and distributions rather than selecting a favorable run.
3. **Analyze WPF construction:** profile the additional construction time and allocations before attempting optimization.
4. **Analyze Blazor allocations:** identify the source of the consistently higher GUI allocation footprint, while preserving equivalent workloads.
5. **Keep the measurement boundaries honest:** add interactive Blazor and visible WPF UX tests only when their adapter contracts and runner requirements are explicitly supported.

## 7. Artifact contents

The uploaded artifact currently contains:

- WPF lifecycle per-sample CSV;
- Blazor BenchmarkDotNet CSV, HTML, and GitHub Markdown reports;
- BenchmarkDotNet execution log;
- captured .NET, host, and timestamp environment manifests.

It does not contain the browser comparison CSV. Keep the workflow artifact as the raw evidence source; this checked-in document is the human-readable interpretation of the values available from that run.

---

**Release/publishing note:** this work is benchmark documentation only. It does not authorize package publication, branch merging, or default-branch changes.
