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
            new("Text", "dashboardHeading", "Workshop dashboard")
        };

        for (var section = 0; section < sections; section++)
        {
            var controls = new List<GuiNode>
            {
                new("Text", $"section{section}Heading", $"Section {section + 1}"),
                new("Button", $"section{section}Save", "Save"),
                new("Warning", $"section{section}Notice", "Changes are saved locally.")
            };
            children.Add(new GuiNode("Panel", $"section{section}", children: controls));
        }

        return new GuiNode("Panel", "dashboard", children: children);
    }

    public static RenderFragment CreateNativeFragment(int sections) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", "dashboard");

        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "id", "dashboardHeading");
        builder.AddContent(4, "Workshop dashboard");
        builder.CloseElement();

        for (var section = 0; section < sections; section++)
        {
            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "id", $"section{section}");

            builder.OpenElement(7, "span");
            builder.AddAttribute(8, "id", $"section{section}Heading");
            builder.AddContent(9, $"Section {section + 1}");
            builder.CloseElement();

            builder.OpenElement(10, "button");
            builder.AddAttribute(11, "id", $"section{section}Save");
            builder.AddContent(12, "Save");
            builder.CloseElement();

            builder.OpenElement(13, "aside");
            builder.AddAttribute(14, "id", $"section{section}Notice");
            builder.AddContent(15, "Changes are saved locally.");
            builder.CloseElement();

            builder.CloseElement();
        }

        builder.CloseElement();
    };
}
