# The Singularity Workshop — GUI Benchmarks

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

See [the measurement protocol](docs/MEASUREMENT_PROTOCOL.md) before adding or interpreting a benchmark.

## Repository layout

- `src/` — executable benchmark hosts and workload definitions.
- `docs/` — workload contracts, measurement protocol, and result interpretation.
- `results/` — checked-in summaries and environment manifests; large raw artifacts belong to workflow artifacts.

## Development

The benchmark suite consumes the GUI repository from its **development** branch while the APIs are under active development. Do not substitute a published NuGet package merely to simplify local setup.

No package publication is part of benchmark development. NuGet publication requires explicit authorization.

## Current status

**Methodology and first milestone are specified. Executable scenarios and measured baselines are not yet complete.** No performance conclusion should be drawn from this repository until reproducible runs and raw artifacts are available.

Tracking: [Issue #1 — Establish user-facing WPF and Blazor performance baselines](https://github.com/TrentBest/TheSingularityWorkshop.Benchmarks.GUI/issues/1).
