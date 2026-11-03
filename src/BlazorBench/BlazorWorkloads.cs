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
                new(GuiKinds.Button, $"section-{section}-save", "Save",
                    properties: new Dictionary<string, string> { ["disabled"] = "false" }),
                new(GuiKinds.Warning, $"section-{section}-notice", "Changes are saved locally.")
            };
            children.Add(new GuiNode(GuiKinds.Panel, $"section-{section}", children: controls));
        }

        return new GuiNode(GuiKinds.Stack, "dashboard",
            properties: new Dictionary<string, string> { ["style:margin"] = "8px", ["style:display"] = "flex" },
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

        var sequence = 6;
        for (var section = 0; section < sections; section++)
        {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "id", $"section-{section}");

            AddText(builder, ref sequence, $"section-{section}-heading", $"Section {section + 1}");
            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "id", $"section-{section}-name");
            builder.AddAttribute(sequence++, "value", $"Value {section + 1}");
            builder.CloseElement();

            builder.OpenElement(sequence++, "button");
            builder.AddAttribute(sequence++, "id", $"section-{section}-save");
            builder.AddAttribute(sequence++, "disabled", false);
            builder.AddContent(sequence++, "Save");
            builder.CloseElement();

            builder.OpenElement(sequence++, "aside");
            builder.AddAttribute(sequence++, "id", $"section-{section}-notice");
            builder.AddContent(sequence++, "Changes are saved locally.");
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private static void AddText(RenderTreeBuilder builder, ref int sequence, string id, string value)
    {
        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "id", id);
        builder.AddContent(sequence++, value);
        builder.CloseElement();
    }
}
