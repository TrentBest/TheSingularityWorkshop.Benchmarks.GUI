using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.Benchmarks.GUI.WpfBench;

internal static class GuiWorkloads
{
    public static GuiNode CreateSemanticTree(int sections)
    {
        var children = new List<GuiNode>();
        for (var section = 0; section < sections; section++)
        {
            var controls = new List<GuiNode>
            {
                Node(GuiKinds.Text, $"section-{section}-heading", $"Section {section + 1}"),
                Node(GuiKinds.TextBox, $"section-{section}-name", $"Name {section + 1}",
                    new Dictionary<string, string> { ["value"] = $"Value {section + 1}", ["width"] = "240" }),
                Node(GuiKinds.Button, $"section-{section}-save", "Save",
                    new Dictionary<string, string> { ["isEnabled"] = "true", ["margin"] = "4" }),
                Node(GuiKinds.Warning, $"section-{section}-notice", "Changes are saved locally.")
            };
            children.Add(new GuiNode(GuiKinds.Panel, $"section-{section}", children: controls));
        }

        return new GuiNode(GuiKinds.Stack, "dashboard",
            text: "Workshop dashboard",
            properties: new Dictionary<string, string>
            {
                ["orientation"] = "Vertical",
                ["margin"] = "8",
                ["isEnabled"] = "true"
            },
            children: children);
    }

    private static GuiNode Node(
        string kind,
        string id,
        string? text = null,
        IReadOnlyDictionary<string, string>? properties = null) =>
        new(kind, id, text, properties: properties);
}
