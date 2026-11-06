<p align="center">
  <a href="https://github.com/TrentBest/TheSingularityWorkshop.Visualize">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop" width="720" />
  </a>
</p>

# The Singularity Workshop — GUI Benchmarks

[![Benchmark harness](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/workflows/benchmark-build.yml/badge.svg?branch=development)](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/workflows/benchmark-build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Last commit](https://img.shields.io/github/last-commit/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/development)](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/commits/development)
[![Sith Policy](assets/sith-policy.svg)](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/actions/workflows/sith-policy.yml)


This repository establishes reproducible performance evidence for the GUI adapters developers actually use.

## First priority: adapter experience

The first benchmark milestone compares:

1. **GUI.WPF** semantic manifestation against equivalent native WPF controls.
2. **GUI.Blazor** semantic manifestation against equivalent native Blazor components, including browser-visible behavior.

The goal is not to declare the abstraction universally faster. It is to show the cost, memory footprint, and update behavior of defined application workloads, with the native comparison and measurement boundary made explicit.

**GUI.Core internal microbenchmarks are a later phase.** They follow the adapter baselines and should help explain them—not displace the user-facing measurements.

## Workload family

Each adapter starts with the same three application slices:

| Scenario | Intended shape |
|---|---|
| Small | Compact settings form / control panel |
| Medium | Multi-section form or dashboard |
| Large | Dense dashboard with nested panels and repeated controls |

The workload specification defines node/control counts, nesting, content, properties, and update sequences. Platform-specific differences are recorded rather than hidden.

## Measurement boundaries

- WPF: distinguish process/application startup, semantic-tree-to-visual-tree manifestation, native control construction, dispatcher-driven updates, steady-state interaction, and teardown.
- Blazor: distinguish component/render-tree work from browser layout, paint, and interaction. Browser-visible results require a real browser run; a render-tree-only microbenchmark is not an end-to-end Blazor UX result.
- Both: report absolute measurements and GUI/native ratios, distributions, variance, allocations/memory where meaningful, and complete environment metadata.

See [the measurement protocol](docs/MEASUREMENT_PROTOCOL.md), [WPF workload definition](docs/WPF_WORKLOADS.md), and [Blazor workload definition](docs/BLAZOR_WORKLOADS.md) before adding or interpreting a benchmark.

## Repository layout

- `src/` — executable benchmark hosts and workload definitions.
- `docs/` — workload contracts, measurement protocol, and result interpretation.
- `results/` — checked-in summaries and environment manifests; large raw artifacts belong to workflow artifacts.

## Development

The benchmark suite consumes the GUI repository from its **development** branch while the APIs are under active development. Do not substitute a published NuGet package merely to simplify local setup.

No package publication is part of benchmark development. NuGet publication requires explicit authorization.

## Repository governance

The permanent branch policy is evaluated by the Sith Policy workflow.

## Current status

**The Blazor render-tree harness builds and passes BenchmarkDotNet Dry smoke runs on Windows CI. WPF uses its dedicated STA lifecycle harness rather than BenchmarkDotNet because the latter's MTA worker model is incompatible with WPF control creation.** The workflow also uploads the smoke-run artifacts for inspection. These Dry jobs verify that the benchmark cases execute; their timings are not performance evidence and must not be reported as baselines.

The browser-hosted GUI/native Blazor static-rendering comparison uses Playwright/Chromium, 30 paired samples per implementation, and alternating order. The WPF suite uses an STA lifecycle harness with five warmups and 30 paired samples; it records construction, Window content attachment, explicit layout/dispatcher work, update, synthetic routed-command dispatch, teardown, and per-thread allocation deltas separately. It deliberately does not show the Window or use physical input, so it is not a visible-paint or user-perceived latency benchmark. The current Blazor renderer does not wire event callbacks, so its browser comparison remains static-rendering only; interaction latency is out of scope until the adapter exposes a tested interaction contract.

### Latest measured findings

[GUI Adapter Benchmark Findings — run 42001741506](docs/ADAPTER_FINDINGS_2026-11-05_RUN_42001741506.md) documents the latest complete hosted capture, including 30-sample WPF lifecycle measurements, Blazor render-tree distributions, environment details, and the validated 60-row browser artifact. In this run, the 18-section Blazor render-tree mean was 8.213 μs GUI versus 9.344 μs native (about 12.1% less time), with higher GUI allocations. The report explicitly distinguishes the new run from earlier browser results and avoids carrying forward stale medians.

[Earlier GUI Adapter Findings — 2026-11-05](docs/ADAPTER_FINDINGS_2026-11-05.md) and [Initial GUI Adapter Findings — 2026-11-04](docs/INITIAL_ADAPTER_FINDINGS_2026-11-04.md) remain as historical evidence.

Tracking: [Issue #1 — Establish user-facing WPF and Blazor performance baselines](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/1).

<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
