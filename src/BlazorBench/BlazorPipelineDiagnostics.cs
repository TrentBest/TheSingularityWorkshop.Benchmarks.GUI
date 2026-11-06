using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.BlazorBench;

/// <summary>
/// Diagnostic decomposition of the GUI.Blazor pipeline. Private-method delegates are
/// bound once in GlobalSetup so reflection is not part of measured operations.
/// These measurements are diagnostic and should be interpreted alongside
/// BlazorRenderTreeBenchmarks, not as replacements for its public-API comparison.
/// </summary>
[MemoryDiagnoser]
public class BlazorPipelineDiagnostics
{
    [Params(2, 6, 18)]
    public int Sections { get; set; }

    private GuiNode _semanticTree = null!;
    private Action<GuiNode> _validate = null!;
    private Action<RenderTreeBuilder, GuiNode, Action<string>?> _renderNode = null!;
    private RenderFragment _preparedFragment = null!;

    [GlobalSetup]
    public void Setup()
    {
        _semanticTree = BlazorWorkloads.CreateSemanticTree(Sections);

        // Bind the exact production methods once, outside the timed region. If the
        // implementation changes these private contracts, fail setup rather than
        // silently benchmarking a copied or incomplete implementation.
        var renderer = typeof(BlazorGuiRenderer);
        var validateMethod = renderer.GetMethod(
            "ValidateUniqueIds",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BlazorGuiRenderer.ValidateUniqueIds was not found.");
        var renderMethod = renderer.GetMethod(
            "RenderNode",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BlazorGuiRenderer.RenderNode was not found.");

        _validate = validateMethod.CreateDelegate<Action<GuiNode>>();
        _renderNode = renderMethod.CreateDelegate<Action<RenderTreeBuilder, GuiNode, Action<string>?>>();
        _preparedFragment = builder => _renderNode(builder, _semanticTree, null);
    }

    [Benchmark(Description = "GUI.Blazor validation only")]
    public void ValidationOnly() => _validate(_semanticTree);

    [Benchmark(Description = "GUI.Blazor fragment creation only")]
    public RenderFragment FragmentCreationOnly() =>
        builder => _renderNode(builder, _semanticTree, null);

    [Benchmark(Description = "GUI.Blazor fragment execution only")]
    public RenderTreeBuilder FragmentExecutionOnly()
    {
        var builder = new RenderTreeBuilder();
        _preparedFragment(builder);
        return builder;
    }

    [Benchmark(Description = "GUI.Blazor public Render plus fragment execution")]
    public RenderTreeBuilder FullPublicPipeline()
    {
        var fragment = BlazorGuiRenderer.Render(_semanticTree);
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder;
    }
}
