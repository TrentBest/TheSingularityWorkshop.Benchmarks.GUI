# Blazor workload definition — v1

## Scope

The first executable Blazor suite measures .NET render-tree construction for the current verified development-branch semantic vocabulary (Panel, Text, Button, and Warning). It invokes the GUI.Blazor RenderFragment and a native RenderFragment into fresh RenderTreeBuilder instances for the same defined workload sizes.

This is not browser-visible rendering, first-interactive latency, layout, paint, network/server latency, WebAssembly startup, or interaction latency. Browser automation remains a required later milestone before making end-user Blazor performance claims.

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

Each section contains a heading, button, and notice. The root is a Panel containing a dashboard heading and section panels. Native and semantic versions emit matching HTML element types, IDs, and visible text.

## Measurement boundary

The benchmark includes fresh RenderTreeBuilder allocation and invocation of the fragment. Fragment creation and semantic-tree creation are in GlobalSetup and are excluded from the timed operation. Both paths use the same runtime and builder type.

Results must be labeled **.NET render-tree construction**. A separate browser-hosted comparison must measure first meaningful render and action-to-visible-completion in a real browser before the project can claim browser-visible Blazor performance.
