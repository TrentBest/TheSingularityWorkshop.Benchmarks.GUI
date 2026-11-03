using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.BlazorBench;

internal static class BlazorWorkloads
{
    public static GuiNode CreateSemanticTree(int sections)
    {
        var children = new List<GuiNode>
        {
            new(GuiKinds.Text, "dashboard-heading", "Workshop dashboard")
        };

        for (var section = 0; section < sections; section++)
        {
            var controls = new List<GuiNode>
            {
                new(GuiKinds.Text, $"section-{section}-heading", $"Section {section + 1}"),
                new(GuiKinds.TextBox, $"section-{section}-name", $"Name {section + 1}",
                    properties: new Dictionary<string, string> { ["value"] = $"Value {section + 1}" }),
                new(GuiKinds.Button, $"section-{section}-save", "Save"),
                new(GuiKinds.Warning, $"section-{section}-notice", "Changes are saved locally.")
            };
            children.Add(new GuiNode(GuiKinds.Panel, $"section-{section}", children: controls));
        }

        return new GuiNode(GuiKinds.Stack, "dashboard",
            properties: new Dictionary<string, string> { ["style:margin"] = "8px" },
            children: children);
    }

    public static RenderFragment CreateNativeFragment(int sections) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", "dashboard");
        builder.AddAttribute(2, "style", "display:flex;flex-direction:column;margin:8px");
        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "id", "dashboard-heading");
        builder.AddContent(5, "Workshop dashboard");
        builder.CloseElement();

        for (var section = 0; section < sections; section++)
        {
            builder.OpenElement(6, "div");
            builder.AddAttribute(7, "id", $"section-{section}");

            AddText(builder, $"section-{section}-heading", $"Section {section + 1}");
            builder.OpenElement(11, "input");
            builder.AddAttribute(12, "id", $"section-{section}-name");
            builder.AddAttribute(13, "value", $"Value {section + 1}");
            builder.CloseElement();

            builder.OpenElement(14, "button");
            builder.AddAttribute(15, "id", $"section-{section}-save");
            builder.AddContent(17, "Save");
            builder.CloseElement();

            builder.OpenElement(18, "aside");
            builder.AddAttribute(19, "id", $"section-{section}-notice");
            builder.AddContent(20, "Changes are saved locally.");
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private static void AddText(RenderTreeBuilder builder, string id, string value)
    {
        builder.OpenElement(8, "span");
        builder.AddAttribute(9, "id", id);
        builder.AddContent(10, value);
        builder.CloseElement();
    }
}
