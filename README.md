# agent-windows-control

Agent-friendly Windows desktop automation primitives built on top of LibreAutomate.

This project is intended to expose reliable local automation actions to coding agents and desktop agents. The first implementation target is a JSON-speaking CLI. An MCP adapter can be added on top of the same core library later.

## Goals

- Provide explicit Windows automation tools for agents: find window, capture window, click, drag/drop, type, wait, and verify.
- Return structured feedback for every action, not just success/failure.
- Separate action intent from delivery mechanism. For example, a click can be semantic invoke, background window message, or physical mouse input.
- Return scaled screenshots that agents can understand and click against without shipping huge high-resolution images.
- Support click previews that draw the actual computed click point before execution.
- Normalize DPI handling by keeping physical desktop pixels, UI logical coordinates, and agent-scaled image coordinates explicit.
- Use LibreAutomate for Windows UI automation, mouse/keyboard input, windows, images, and related desktop APIs.
- Keep the core automation code independent from the transport layer, so CLI and MCP can share the same behavior.

## Planned architecture

```text
AgentWindowsControl.Core
  Shared action models, result models, safety checks, screenshot scaling, coordinate mapping, and LibreAutomate integration.

AgentWindowsControl.Cli
  JSON CLI entrypoint for humans, scripts, CI, and agents with shell access.

AgentWindowsControl.Mcp
  Future MCP adapter that maps MCP tool calls to the same Core APIs.
```

## LibreAutomate reference

This project references the `LibreAutomate` NuGet package:

```xml
<PackageReference Include="LibreAutomate" Version="1.15.0" />
```

The current NuGet package supports `net8.0-windows7.0` and `net9.0-windows7.0`, so this project currently targets `net9.0-windows`.

LibreAutomate project links:

- Website: https://www.libreautomate.com/
- NuGet: https://www.nuget.org/packages/LibreAutomate

## License compatibility

LibreAutomate is licensed under the MIT License. MIT is compatible with this project's MIT License, so this repository also uses MIT.

When distributing this project with LibreAutomate binaries, source, or substantial portions, keep the LibreAutomate copyright notice and MIT license text. If LibreAutomate source is vendored or modified, include the upstream MIT license notice with the vendored copy.

See `THIRD_PARTY_NOTICES.md` for the dependency notice.

## Coordinate model

Windows automation must not mix coordinate spaces. The project contract uses three explicit spaces:

- `desktopPhysical`: real desktop pixels used for screenshots, physical mouse input, and UI Automation bounding rectangles.
- `windowLogical`: target-window logical coordinates when interacting with APIs or message delivery that depend on the target HWND's DPI awareness.
- `agentImage`: scaled screenshot pixels shown to the agent.

Every screenshot response must include the transform from `agentImage` back to `desktopPhysical`. Every message-based action must document whether it expects `desktopPhysical`, `windowLogical`, or client coordinates.

See `docs/dpi-coordinate-model.md` for the full policy.

## Window-relative actions

Click requests may use window-relative coordinates, such as `clientPhysical` or `clientFraction`, but execution must normalize them once through a fresh window geometry snapshot.

Previewable click flow:

```text
find_window -> capture_window -> preview_click -> click with revalidation
```

`preview_click` must draw the final computed click point and target bounds on the scaled window screenshot so an agent can inspect the planned operation before executing it.

## Early CLI shape

The CLI should prefer JSON input and JSON output to avoid fragile shell parsing:

```powershell
awc tools
awc license
awc call --file request.json
```

Example future window capture request:

```json
{
  "tool": "capture_window",
  "arguments": {
    "window": {
      "title": "Untitled - Notepad"
    },
    "image": {
      "maxEdge": 1280,
      "maxPixels": 1200000
    }
  }
}
```

The response must include a coordinate transform so an agent can click in the scaled image and the tool can map that point back to desktop coordinates.

Example future click request:

```json
{
  "tool": "click",
  "arguments": {
    "target": {
      "windowTitle": "Untitled - Notepad",
      "name": "Save",
      "controlType": "Button"
    },
    "mode": "auto",
    "expect": {
      "type": "window_closes",
      "timeoutMs": 2000
    }
  }
}
```

Example future response:

```json
{
  "ok": true,
  "selectedStrategy": "physical",
  "targetFound": true,
  "verification": {
    "passed": true,
    "reason": "Dialog closed after click"
  },
  "warnings": []
}
```
