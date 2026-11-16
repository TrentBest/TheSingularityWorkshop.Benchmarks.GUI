# Browser Artifact Analysis — 2026-11-05, Run 42001741506

> **Evidence status:** secondary analysis of the retained browser CSV from one hosted Windows CI run. This is not a new measurement run, not a cross-run baseline, and not a user-perceived latency benchmark.

- Source workflow: [Measure GUI adapter baselines — run 42001741506](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/runs/42001741506)
- Source artifact: [gui-adapter-measurements-42001741506](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/runs/42001741506/artifacts/13402019937)
- Source artifact SHA-256: `a53cab1c56bfe83df33c9da44d11e472297fe241752ec2c083a1f2f3b463748c`
- CSV: `results/browser/blazor-browser.csv`
- Browser: Chromium via Playwright (version is not recorded in this CSV)
- Viewport: 1264 × 720 CSS pixels
- Samples: 30 native + 30 GUI, paired by sample index, alternating execution order

## 1. Results

All values below are milliseconds. The primary summary retains all 30 observations per implementation.

| Browser metric | Native median | GUI median | Native p90 | GUI p90 | Paired median difference (GUI − native) |
|---|---:|---:|---:|---:|---:|
| Time to first byte (TTFB) | 2.45 | 2.50 | 3.00 | 2.73 | 0.00 |
| DOM content loaded | 12.55 | 12.00 | 25.02 | 24.51 | +0.05 |
| Load event | 13.15 | 12.65 | 25.02 | 24.51 | −0.05 |

Paired difference direction: negative means the GUI observation completed sooner; positive means native completed sooner.

The paired sample counts are mixed:
- TTFB: GUI faster in 13 pairs, tied in 3, native faster in 14.
- DOM content loaded: GUI faster in 14 pairs, tied in 1, native faster in 15.
- Load event: GUI faster in 15 pairs, tied in 1, native faster in 14.

There is no consistent browser-timing winner in this capture. The median differences are effectively zero at the resolution of the recorded data.

## 2. Cold-first-observation sensitivity

The first native observation is a pronounced cold-start outlier: TTFB 1006.5 ms, DOM content loaded 1019.2 ms, and load event 1020.0 ms. The corresponding first GUI observation is 6.8 ms TTFB and 24.6 ms load.

This single observation materially inflates the native arithmetic mean. To show sensitivity without hiding the observation, the following table presents a secondary diagnostic with sample 1 excluded from both implementations. This is **not** the primary result and is not a post-hoc replacement for the all-sample summary.

| Metric | Native mean, all 30 | GUI mean, all 30 | Native mean, samples 2–30 | GUI mean, samples 2–30 |
|---|---:|---:|---:|---:|
| TTFB | 35.94 | 2.61 | 2.47 | 2.47 |
| DOM content loaded | 49.79 | 16.04 | 16.36 | 15.74 |
| Load event | 50.19 | 16.44 | 16.74 | 16.16 |

After removing sample 1 from both sides for this sensitivity check, the medians remain almost identical: TTFB 2.4 ms native vs 2.5 ms GUI; DOM content loaded 12.2 ms native vs 12.0 ms GUI; load event 12.7 ms native vs 12.6 ms GUI.

The first native observation should be investigated as a runner/browser/server warm-up effect. It must not be silently discarded. Future runs should define cold-start and warm steady-state protocols separately, and record per-sample execution order and readiness markers.

## 3. Interpretation

What this artifact supports:
- The static browser comparison completed 30 paired observations per implementation at the same recorded viewport.
- After warm-up, the two static pages have very similar browser timing distributions in this one hosted capture.
- The browser timing data does not establish a meaningful GUI advantage or penalty.
- A large first native observation dominates the all-sample arithmetic mean, demonstrating why distributions and cold/warm separation matter.

What it does **not** support:
- Interactive Blazor event latency or input-to-visible-response.
- Browser paint completion or user-perceived responsiveness.
- A claim that GUI.Blazor is faster than native Blazor in the browser.
- Cross-run repeatability, cross-machine generalization, or a statistically stable performance baseline.

This browser test compares static rendered output. It does not measure GUI.Blazor event callback behavior, and the adapter currently has no tested event callback contract. Do not interpret browser load timing as GUI.Core or adapter-only execution time.

## 4. Next measurement improvements

1. Split cold browser startup from warmed repeated navigation; preserve both results.
2. Record Playwright and Chromium versions, browser launch arguments, server process identity, and per-sample order in the CSV.
3. Add an explicit server-ready signal before measuring page navigation, so server startup is not accidentally assigned to whichever implementation is visited first.
4. Capture browser performance entries and traces if the intended claim includes layout or paint; the current CSV records navigation lifecycle timings only.
5. Repeat the complete workflow in multiple independent hosted runs before making a browser-performance claim.
6. Keep static rendering distinct from interactive Blazor measurements until a real adapter event contract exists.

## 5. Overall adapter context

The same run's .NET render-tree measurements showed workload-dependent timing and higher GUI allocations; its WPF STA lifecycle measurements were generally near native, with consistently higher GUI construction allocations. Those findings are summarized in [the main adapter findings report](ADAPTER_FINDINGS_2026-11-05_RUN_42001741506.md).

The browser artifact adds an important qualification: **the browser-visible static timing comparison is essentially at parity in this capture**. The strongest current optimization lead remains managed allocation and construction/validation overhead, not a demonstrated browser-load bottleneck.

---

**Governance:** this is an analysis of an existing artifact committed on `development`. It does not change `master`, publish packages, merge PRs, or alter branch policy.
