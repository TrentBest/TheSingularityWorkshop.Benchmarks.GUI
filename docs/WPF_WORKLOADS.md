# WPF workload definition — v1

## Scope

The first executable suite measures **warm in-process control-tree construction** for the current verified semantic vocabulary. It compares WpfGuiRenderer.Render(GuiNode) with direct native WPF construction.

It does not yet measure process startup, window attachment, dispatcher/layout completion, painting, interaction, or teardown. Those require a dedicated STA desktop-host harness and are separate follow-up milestones.

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

This initial harness compares construction only. It returns the root element to prevent the operation from being reduced to a discarded call, but it does not attach it to a Window or force layout.

## Result naming

Benchmark methods are intentionally named NativeWpfConstruction and GuiWpfManifestation. The report must not label these results as full UI rendering or user-perceived latency.
