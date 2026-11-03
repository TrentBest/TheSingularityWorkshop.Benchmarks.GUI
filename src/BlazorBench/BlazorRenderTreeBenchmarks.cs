using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.BlazorBench;

/// <summary>
/// Measures .NET render-tree construction only; it does not measure browser layout, paint, JS, or interaction.
/// </summary>
[MemoryDiagnoser]
public class BlazorRenderTreeBenchmarks
{
    [Params(2, 6, 18)]
    public int Sections { get; set; }

    private RenderFragment _guiFragment = null!;
    private RenderFragment _nativeFragment = null!;

    [GlobalSetup]
    public void Setup()
    {
        _guiFragment = BlazorGuiRenderer.Render(BlazorWorkloads.CreateSemanticTree(Sections));
        _nativeFragment = BlazorWorkloads.CreateNativeFragment(Sections);
    }

    [Benchmark(Baseline = true, Description = "Native Blazor render-tree construction")]
    public RenderTreeBuilder NativeBlazorRenderTree()
    {
        var builder = new RenderTreeBuilder();
        _nativeFragment(builder);
        return builder;
    }

    [Benchmark(Description = "GUI.Blazor semantic render-tree construction")]
    public RenderTreeBuilder GuiBlazorRenderTree()
    {
        var builder = new RenderTreeBuilder();
        _guiFragment(builder);
        return builder;
    }
}
