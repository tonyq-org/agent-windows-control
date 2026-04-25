namespace AgentWindowsControl.Core;

public sealed record ToolDescriptor(
    string Name,
    string Purpose,
    string[] PlannedModes,
    string[] Feedback);

public static class ToolCatalog
{
    public static readonly ToolDescriptor[] All =
    [
        new(
            Name: "find_window",
            Purpose: "Locate a desktop window and return its handle, bounds, process, and observation handles.",
            PlannedModes: ["title", "process", "class_name", "focused"],
            Feedback: ["window handle", "bounds", "DPI scale", "visibility", "foreground state"]),

        new(
            Name: "capture_window",
            Purpose: "Capture a found window as an agent-readable scaled screenshot with coordinate mapping.",
            PlannedModes: ["fit_max_edge", "fit_max_pixels", "crop_region", "full_resolution_artifact"],
            Feedback: ["scaled image path", "original bounds", "scale factor", "coordinate transform", "optional full-resolution artifact path"]),

        new(
            Name: "click",
            Purpose: "Activate a target UI element with explicit delivery strategy reporting.",
            PlannedModes: ["semantic", "background_message", "physical", "auto"],
            Feedback: ["selected strategy", "hit-test result", "post-action UI change", "verification result"]),

        new(
            Name: "drag_and_drop",
            Purpose: "Move UI elements or data between source and target locations.",
            PlannedModes: ["physical_mouse", "drop_files", "auto"],
            Feedback: ["source bounds", "target bounds", "input trace", "verification result"]),

        new(
            Name: "type_text",
            Purpose: "Enter text into the active or selected UI target.",
            PlannedModes: ["clipboard_paste", "physical_keyboard", "control_value"],
            Feedback: ["target before state", "text delivery strategy", "post-action value check"])
    ];
}
