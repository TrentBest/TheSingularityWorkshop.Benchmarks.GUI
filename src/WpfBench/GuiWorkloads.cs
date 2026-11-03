using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

internal static class GuiWorkloads
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

        return new GuiNode("Column", "dashboard", children: children);
    }
}
