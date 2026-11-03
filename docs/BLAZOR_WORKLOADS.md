# Blazor workload definition — v2

## Scope

The Blazor suite has two distinct measurement layers:

1. **.NET render-tree construction** — BenchmarkDotNet compares the GUI.Blazor RenderFragment with a native RenderFragment using fresh RenderTreeBuilder instances.
2. **Browser-hosted static HTML comparison** — a small ASP.NET Core host renders the same GUI and native components to HTML using Blazor's HtmlRenderer. Playwright loads each response in Chromium and records browser navigation milestones plus a forced style/layout read.

The browser harness currently exercises the large workload (18 sections, 74 semantic nodes) and collects 15 samples per implementation. It records TTFB, DOMContentLoaded, load completion, and resulting layout dimensions. The workflow captures the output as an artifact.

## Scenario matrix

| Parameter | Small | Medium | Large |
|---|---:|---:|---:|
| Sections | 2 | 6 | 18 |
| Root nodes | 1 | 1 | 1 |
| Dashboard heading | 1 | 1 | 1 |
| Section panels | 2 | 6 | 18 |
| Controls per section | 3 | 3 | 3 |
| Total semantic nodes | 10 | 26 | 74 |
| Maximum tree depth | 3 | 3 | 3 |

Each section contains a heading, button, and notice. The root is a Panel containing a dashboard heading and section panels. Native and semantic versions are intended to emit matching HTML element types, IDs, and visible text.

## Measurement boundaries and limitations

The .NET benchmark includes the adapter entry operation, fresh RenderTreeBuilder allocation, and fragment invocation. Semantic-tree creation is in GlobalSetup and excluded. Results must be labeled **.NET render-tree construction**.

The browser harness measures a loopback HTTP request whose response is server-rendered static HTML. Its timings include server-side HtmlRenderer work, HTTP response delivery, browser document parsing, and browser layout calculation. They are useful as an initial browser-visible delivery comparison, but they are **not** measurements of interactive Blazor Server/WebAssembly startup, component event dispatch, physical display paint, or user interaction latency. The layout read forces style/layout calculation but does not prove that pixels were physically presented.

The current GUI.Blazor renderer does not wire event callbacks. Buttons are therefore static markup in this workload; no button interaction is simulated or reported. Browser measurements use Chromium headless at a 1280×800 viewport and device scale factor 1. The harness currently runs 15 samples per path; treat results as preliminary until repeated runs and variance analysis are available.

## Next fidelity steps

- Add small and medium browser scenarios, randomized or alternating implementation order, and warm-up samples.
- Separate server render duration from network/browser navigation timings.
- Add an interactive Blazor host only after the adapter's event contract is implemented and tested.
- Use an interactive desktop/display-backed environment before reporting physical paint or input-to-presentation latency.
