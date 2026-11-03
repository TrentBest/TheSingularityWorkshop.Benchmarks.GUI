# GUI Adapter Measurement Protocol

## 1. Purpose and ordering

The benchmark must answer the application developer's question first: what does it cost to use GUI.WPF or GUI.Blazor for a useful interface, compared with building the same interface with the platform's native controls?

The order is deliberate:

1. WPF adapter versus native WPF.
2. Blazor adapter versus native Blazor, including browser-visible behavior.
3. Only after those baselines: GUI.Core construction, traversal, identity lookup, observation, and multi-view composition.

A Core-only benchmark cannot stand in for adapter experience.

## 2. Workload contract

Use three named workload sizes for each adapter.

| Workload | Description | Design intent |
|---|---|---|
| Small | Compact settings form | Typical small application surface |
| Medium | Multi-section form or dashboard | Several nested groups and mixed controls |
| Large | Dense dashboard | Repeated controls, nested panels, representative content |

For every workload, retain a machine-readable manifest describing:

- semantic node count and native control/component count;
- kind/type counts;
- maximum and average nesting depth;
- text length and image/media placeholders;
- property/style set;
- initial state;
- exact update and interaction sequence;
- excluded capabilities and platform-specific differences.

The native and GUI cases must implement the same visible information and interaction sequence wherever possible. If the current semantic vocabulary cannot represent an equivalent native feature, omit it from both cases or explicitly document the difference. Do not quietly benchmark different interfaces.

## 3. WPF protocol

The current GUI.WPF implementation exposes two distinct paths: semantic manifestation from a Core GuiNode and platform-specific native WPF builders. The first comparison is semantic manifestation versus directly constructed native WPF controls.

Measure separately:

1. **Cold application startup** — process launch through a defined ready signal. Report separately from steady-state microbenchmarks.
2. **Warm manifestation/construction** — build the semantic tree and call WpfGuiRenderer.Render versus construct the matching native control tree.
3. **Attach and layout** — attach to a visible host and allow dispatcher/layout work to reach a defined completion point.
4. **Update** — apply the same state/content changes to both trees on the dispatcher.
5. **Steady-state interaction** — measure a defined interaction and completion signal, not just event-handler invocation.
6. **Teardown** — detach and release the visual tree; distinguish deterministic disposal from GC observations.

WPF measurements must run on Windows in an STA-capable process. Record Windows version, CPU, .NET runtime, display/DPI configuration, build mode, and whether a visible desktop session was used.

**Runner constraint:** a hosted Windows CI process may run in a non-interactive session. It can validate WPF construction, dispatcher/layout completion, and harness correctness, but it must not be described as a visible-desktop paint/input benchmark unless the runner is demonstrably attached to an interactive desktop with a recorded display configuration. Keep interactive WPF UX measurements as a separately labeled local/desktop run until an interactive runner is deliberately provisioned. Do not add a self-hosted runner requirement or expose a machine to the repository without an explicit security review.

BenchmarkDotNet is appropriate for isolated warm in-process operations when its process/affinity model is compatible with WPF. Use a separate harness for process startup and visible UI interaction. Never combine those boundaries into one unexplained number.

## 4. Blazor protocol

The current GUI.Blazor adapter returns a RenderFragment from a GuiNode. That is not itself a complete browser experience. Use two explicitly named measurement layers:

### 4.1 Render-tree/component work

Measure the work to invoke the GUI RenderFragment and the equivalent native component/render fragment under the same renderer and host conditions. This can isolate .NET-side component/render-tree work.

Label this result **render-tree/component work**, not browser rendering or end-to-end Blazor performance.

### 4.2 Browser-visible experience

Host the GUI and native versions in the same application, hosting mode, runtime, and browser. Use browser automation/instrumentation to capture:

- navigation/process startup separately from first interactive readiness;
- first meaningful render and subsequent settled layout/paint;
- scripted input/update latency from action to visible completion;
- repeated update throughput and responsiveness;
- browser memory where reliable and comparable.

Record Blazor hosting mode (for example, WebAssembly or server/interactive server), .NET runtime, browser and version, OS, CPU, viewport, device scale factor, and automation/instrumentation versions.

The browser harness should serve GUI and native pages from the same host build and use the same Playwright browser context, viewport, device scale factor, and scripted action sequence. Capture browser traces and page performance entries alongside the .NET-side measurements. If server hosting is used, capture server-side request/render timing separately from browser action-to-visible-completion; do not collapse them into a single unexplained latency.

Do not attribute browser layout, paint, network, or server latency to GUI.Core. Record them as separate boundaries when instrumentation permits.

## 5. Statistical and reporting rules

- Use Release builds for published measurements.
- Warm up JIT/runtime paths; report cold-start separately.
- Run enough independent launches and iterations to characterize variance. Record the exact run policy.
- Report median and distribution/percentiles where the harness supports them; include sample count and variance.
- Include absolute values and GUI/native ratios. State the ratio direction explicitly.
- Preserve raw BenchmarkDotNet output, browser traces, logs, and machine-readable summaries as workflow artifacts.
- Never select a single favorable run or suppress outliers without a documented rule.
- Do not publish a headline performance claim from an unrepeatable local run.

Every result must carry an environment manifest: commit SHAs for both repositories, branch/ref, OS, CPU, RAM, .NET SDK/runtime, configuration, workload manifest version, browser/driver versions when applicable, and run timestamp.

## 6. Interpretation

A slower adapter result is still useful: it identifies the cost of semantic construction, mapping, or runtime integration. A faster result does not prove universal superiority. Performance is one axis alongside portability, semantic clarity, maintainability, accessibility, and developer experience.

The benchmark report must state what was measured, what was excluded, and what conclusion the evidence supports.

## 7. Current implementation boundary

The benchmark consumes the GUI repository's `development` branch. The source audit for the current benchmark revision confirms that both `tests/WPF.Tests` and `tests/Blazor.Tests` exist, alongside `tests/Adapter.Conformance.Tests`. Test-project existence is not proof that every adapter capability is complete; benchmark workloads must still stay within the exact behavior exercised by the selected source and conformance tests.

The current `BlazorGuiRenderer` maps semantic kinds to HTML elements and emits node identity, source, string properties, text, and child elements. Its current implementation does not attach Blazor event callbacks to rendered controls. Therefore the initial browser comparison must measure equivalent static rendering and only add interaction scenarios after an explicitly supported, tested interaction contract exists. Do not label a synthetic browser click on a non-functional adapter button as an adapter interaction benchmark.

The current `WpfGuiRenderer` manifests semantic nodes as WPF controls. `WpfWindowBuilder` builds a host window but deliberately does not show it or take ownership of its lifetime. The host/lifecycle harness must own showing, dispatcher synchronization, closing, and cleanup, and must compare the same content and host configuration for the native WPF case.

The WPF executable suite measures warm control-tree construction and a separate STA host/lifecycle path. The latter records tree construction, Window content attachment, explicit measure/arrange/layout and dispatcher drain, content update, synthetic routed-command dispatch, and teardown as separate per-sample phases. It uses an unshown Window and synthetic event; it does not measure visible paint, physical input, or user-perceived latency. The Blazor suite measures .NET render-tree construction plus a browser-hosted static HTML comparison. The browser harness does not yet measure interactive Blazor hosting or adapter event dispatch. See WPF_WORKLOADS.md and BLAZOR_WORKLOADS.md for exact boundaries.

The benchmark project must not imply that incomplete input, accessibility, capability-negotiation, event wiring, or lifecycle contracts are covered.

## 8. Deferred Core suite

Only after the adapter baselines are reproducible should the suite add internal measurements for:

- semantic tree construction;
- recursive traversal;
- stable identity lookup;
- property and child observation;
- multi-view composition and update;
- serialization/reconstruction, once those contracts are implemented.

These explain the user-facing results; they do not replace them.
