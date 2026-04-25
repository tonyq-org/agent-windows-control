# Automation Contract

The agent-facing API should be closed-loop:

```text
observe -> act -> verify -> report evidence
```

Action tools must return structured feedback that lets an agent decide whether the action actually worked.

## Window screenshots

After locating a window, the tool should be able to capture an agent-readable screenshot without returning an oversized desktop image.

Screenshot coordinates and click coordinates must follow the DPI policy in `docs/dpi-coordinate-model.md`.

The capture response must include:

- scaled image artifact path
- original window bounds in desktop coordinates
- scaled image size
- scale factor
- crop origin
- coordinate transform from scaled image point to desktop point
- DPI awareness and DPI value for the target window when available
- optional full-resolution artifact path when explicitly requested

Default capture policy:

- capture only the target window or requested region, not the whole desktop
- fit the scaled image within a configurable max edge, such as 1280 pixels
- fit the scaled image under a configurable max pixel count, such as 1.2 megapixels
- preserve enough detail for UI text and controls to remain clickable
- never lose the mapping back to original desktop coordinates

Example response shape:

```json
{
  "ok": true,
  "window": {
    "title": "Untitled - Notepad",
    "bounds": { "x": 100, "y": 80, "width": 1600, "height": 1000 }
  },
  "image": {
    "path": "artifacts/screens/notepad.scaled.png",
    "width": 1280,
    "height": 800,
    "scale": 0.8,
    "cropOrigin": { "x": 100, "y": 80 }
  },
  "dpi": {
    "windowDpi": 144,
    "scaleFrom96": 1.5,
    "awareness": "per_monitor_v2"
  },
  "coordinateTransform": {
    "scaledToDesktop": "desktopX = cropOrigin.x + scaledX / scale; desktopY = cropOrigin.y + scaledY / scale"
  }
}
```

## Click strategies

Do not treat "left click" as one implementation. A click request has a delivery strategy:

- `semantic`: invoke a control through UI Automation, a control-specific API, or a safe semantic action.
- `background_message`: send a targeted window message such as mouse down/up to an HWND.
- `physical`: move the real pointer and send physical mouse input.
- `auto`: choose one of the above, then report the selected strategy and why.

High-risk actions should avoid silent `auto` behavior and should include explicit verification.

## Window-relative click

Click requests may be expressed relative to a target window. The API must name the coordinate space explicitly.

Example:

```json
{
  "tool": "click",
  "arguments": {
    "window": {
      "title": "Untitled - Notepad"
    },
    "point": {
      "space": "clientFraction",
      "x": 0.5,
      "y": 0.2
    },
    "mode": "physical"
  }
}
```

Execution rule:

```text
input point -> fresh WindowGeometry -> action-native coordinate space -> execute
```

Do not repeatedly convert through multiple spaces. For physical input, normalize to `desktopPhysical`. For message delivery, normalize to the target HWND client coordinate space expected by the message.

## Click preview

`preview_click` plans a click without executing it. It should return both structured coordinates and a scaled screenshot with overlays.

Example request:

```json
{
  "tool": "preview_click",
  "arguments": {
    "window": {
      "title": "Untitled - Notepad"
    },
    "point": {
      "space": "clientPhysical",
      "x": 120,
      "y": 40
    },
    "mode": "physical"
  }
}
```

Example response:

```json
{
  "ok": true,
  "willClick": {
    "desktopPhysical": { "x": 228, "y": 161 },
    "clientPhysical": { "x": 120, "y": 40 },
    "agentImage": { "x": 102, "y": 65 }
  },
  "preview": {
    "path": "artifacts/previews/click-001.png",
    "scaleFromDesktopPhysical": 0.8,
    "overlays": ["frame_bounds", "client_bounds", "target_bounds", "click_crosshair"]
  },
  "geometryVersion": {
    "hwnd": "0x0012049A",
    "bounds": "100,80,1600,1000",
    "dpi": 144
  }
}
```

The subsequent `click` must revalidate the window handle, bounds, DPI, and hit-test before sending input. A preview image is evidence for the agent, not permission to reuse stale physical coordinates.

## Required feedback

Action results should include:

- target lookup result
- target bounds and enabled/visible state
- selected delivery strategy
- hit-test result when physical or message input is used
- input trace summary
- post-action observation
- verification result
- warnings and confidence

## Drag/drop

Drag/drop should be split into:

- `mouse_drag`: physical pointer drag from one point to another.
- `drag_element_to_element`: locate source and target elements, then drag between computed points.
- `drop_files`: data-oriented file drop, implemented with the most reliable available Windows mechanism.

This avoids pretending that all drag/drop operations are the same.
