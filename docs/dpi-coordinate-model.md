# DPI and Coordinate Model

Windows automation must treat DPI as a coordinate-system problem, not as a rendering detail. The same widget can have different widths depending on whether the caller is using physical pixels, window logical coordinates, or a scaled image shown to an agent.

## Coordinate spaces

Use these names in API contracts and action results:

| Space | Meaning | Typical source | Typical use |
| --- | --- | --- | --- |
| `desktopPhysical` | Real desktop pixels. Origin is the virtual desktop origin. | screenshots, UI Automation rectangles, physical cursor/input | screenshots, `SendInput`, hit-testing, visual grounding |
| `windowLogical` | Coordinates as interpreted by the target window's DPI awareness. | target HWND APIs, message-based calls | `PostMessage`, client-area message coordinates, app-specific APIs |
| `clientPhysical` | Physical pixels relative to a window client area. | converted from `desktopPhysical` with window/client bounds | hit-test and client-relative physical calculations |
| `agentImage` | Scaled screenshot pixels returned to the agent. | generated capture artifact | model visual input and point selection |

Do not pass coordinates between these spaces without recording the transform.

## Process DPI awareness

The automation process should be Per-Monitor DPI Aware V2 where possible. This keeps the process in the actual monitor coordinate space and reduces Windows coordinate virtualization.

Startup policy:

1. Set process/thread DPI awareness before creating windows or taking screenshots.
2. Prefer Per-Monitor DPI Aware V2.
3. Record the actual awareness context in diagnostics.
4. Treat any failure to set awareness as a warning in action results.

## Window observation

When `find_window` returns a target, include:

```json
{
  "hwnd": "0x0012049A",
  "boundsDesktopPhysical": { "x": 100, "y": 80, "width": 1600, "height": 1000 },
  "clientBoundsDesktopPhysical": { "x": 108, "y": 121, "width": 1584, "height": 871 },
  "dpi": {
    "windowDpi": 144,
    "scaleFrom96": 1.5,
    "awareness": "per_monitor_v2"
  }
}
```

`width` is never enough by itself. A result must say which coordinate space owns that width.

## Screenshot scaling

Window capture should return a scaled image for the agent and a precise mapping back to desktop coordinates.

```json
{
  "image": {
    "space": "agentImage",
    "path": "artifacts/screens/notepad.scaled.png",
    "width": 1280,
    "height": 800,
    "scaleFromDesktopPhysical": 0.8,
    "cropOriginDesktopPhysical": { "x": 100, "y": 80 }
  },
  "coordinateTransform": {
    "agentImageToDesktopPhysical": {
      "desktopX": "cropOriginDesktopPhysical.x + agentX / scaleFromDesktopPhysical",
      "desktopY": "cropOriginDesktopPhysical.y + agentY / scaleFromDesktopPhysical"
    }
  }
}
```

If the agent chooses point `(640, 400)` in the scaled image above:

```text
desktopX = 100 + 640 / 0.8 = 900
desktopY = 80 + 400 / 0.8 = 580
```

The physical click point is therefore `(900, 580)` in virtual desktop coordinates.

## Window-relative input

Actions may accept window-relative coordinates, but the coordinate space must be explicit.

Supported point spaces:

| Space | Example | Conversion target |
| --- | --- | --- |
| `desktopPhysical` | `{ "x": 900, "y": 580 }` | already physical desktop pixels |
| `clientPhysical` | `{ "x": 120, "y": 40 }` | add `clientBoundsDesktopPhysical` origin |
| `clientFraction` | `{ "x": 0.5, "y": 0.2 }` | multiply by client physical width/height, then add origin |
| `agentImage` | `{ "x": 640, "y": 400 }` | apply screenshot transform to `desktopPhysical` |
| `windowLogical` | `{ "x": 80, "y": 27 }` | convert through target HWND DPI/client coordinate rules |

Examples:

```text
clientPhysical -> desktopPhysical:
desktopX = clientBoundsDesktopPhysical.x + clientX
desktopY = clientBoundsDesktopPhysical.y + clientY

clientFraction -> desktopPhysical:
desktopX = clientBoundsDesktopPhysical.x + clientBoundsDesktopPhysical.width * fractionX
desktopY = clientBoundsDesktopPhysical.y + clientBoundsDesktopPhysical.height * fractionY
```

Use a single fresh `WindowGeometry` snapshot per action. Do not convert coordinates repeatedly across spaces, because rounding and DPI virtualization can accumulate errors.

## Widget bounds

For every UI target, return bounds in all spaces that are relevant to the selected action strategy.

Example:

```json
{
  "target": {
    "name": "Save",
    "controlType": "Button",
    "boundsDesktopPhysical": { "x": 420, "y": 88, "width": 74, "height": 28 },
    "boundsAgentImage": { "x": 256, "y": 64, "width": 59, "height": 22 },
    "boundsWindowLogical": { "x": 280, "y": 59, "width": 49, "height": 19 }
  }
}
```

Rules:

- `boundsDesktopPhysical` is the source of truth for screenshots, visual matching, and physical mouse input.
- `boundsAgentImage` is derived from the screenshot transform and exists only to help the agent reason over the scaled image.
- `boundsWindowLogical` is required only when a strategy needs target-window logical or client coordinates.

## Action strategy implications

### Physical input

Use `desktopPhysical`.

Flow:

1. Locate target bounds in `desktopPhysical`.
2. Convert agent-selected point from `agentImage` to `desktopPhysical` if needed.
3. Hit-test the physical point.
4. Send physical input.
5. Verify post-state.

### Semantic invoke

Prefer element identity over coordinates. DPI is mostly diagnostic unless the fallback needs physical input.

### Background message

Do not send desktop screenshot coordinates directly to `PostMessage`.

Flow:

1. Start from `desktopPhysical`.
2. Convert to target HWND client coordinates.
3. Convert to the coordinate space expected by that target's DPI awareness when required.
4. Record the conversion and the DPI values used.
5. Send the message only if the action contract allows background delivery.

## Verification

Every action result involving coordinates should include:

- input coordinate space
- output coordinate space
- DPI value used for conversion
- target HWND used for conversion
- conversion formula or transform fields
- hit-test result before action
- screenshot or UI tree observation after action

## Known pitfalls

- A screenshot is physical pixels, while a target app may interpret message coordinates in its own logical/client coordinate system.
- Different monitors can have different DPI values.
- Moving a window between monitors can change its DPI.
- DPI-unaware target processes may receive virtualized coordinates.
- A scaled image shown to an agent is a fourth coordinate space; never use agent image pixels directly as desktop pixels.
- Rounding can move small widgets by one or more pixels. Prefer target center points and include hit-testing.

## References

- Microsoft UI Automation screen scaling guidance: https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-and-screen-scaling
- Microsoft per-monitor DPI coordinate conversion: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-logicaltophysicalpointforpermonitordpi
