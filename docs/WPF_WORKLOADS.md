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
| Controls per section | 4 | 4 | 4 |
| Total semantic nodes | 11 | 31 | 91 |
| Maximum tree depth | 3 | 3 | 3 |

Each section contains a heading, a text box with a value and width, a save button with enabled/margin properties, and a warning message. The root contains a dashboard heading and vertical orientation/margin/enabled properties.

## Equivalence notes

The semantic and native paths create the same visible content and corresponding WPF element types for the represented subset: StackPanel, Grid, TextBlock, TextBox, Button, and Border/TextBlock warning. WPF's semantic renderer currently maps Warning to a Border containing a TextBlock.

This initial harness compares construction only. It returns the root element to prevent the operation from being reduced to a discarded call, but it does not attach it to a Window or force layout.

## Result naming

Benchmark methods are intentionally named NativeWpfConstruction and GuiWpfManifestation. The report must not label these results as full UI rendering or user-perceived latency.
