using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.BlazorBench;

/// <summary>
/// Measures adapter invocation plus .NET render-tree construction only; it does not measure browser layout, paint, JS, or interaction.
/// </summary>
[MemoryDiagnoser]
public class BlazorRenderTreeBenchmarks
{
    [Params(2, 6, 18)]
    public int Sections { get; set; }

    private GuiNode _semanticTree = null!;

    [GlobalSetup]
    public void Setup() => _semanticTree = BlazorWorkloads.CreateSemanticTree(Sections);

    [Benchmark(Baseline = true, Description = "Native Blazor render-tree construction")]
    public RenderTreeBuilder NativeBlazorRenderTree()
    {
        RenderFragment fragment = BlazorWorkloads.CreateNativeFragment(Sections);
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder;
    }

    [Benchmark(Description = "GUI.Blazor semantic render-tree construction")]
    public RenderTreeBuilder GuiBlazorRenderTree()
    {
        RenderFragment fragment = BlazorGuiRenderer.Render(_semanticTree);
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder;
    }
}
