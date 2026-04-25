# Elevation Policy

Windows blocks or limits some automation across integrity levels. A non-elevated agent helper may fail to inspect or control elevated windows, services, installers, administrator consoles, or secure desktop UI.

This project should support elevation, but elevation must be intentional and observable.

## Policy

Use these elevation modes in tool requests and configuration:

| Mode | Meaning |
| --- | --- |
| `never` | Do not elevate. Fail with an actionable error if elevation is required. |
| `whenRequired` | Elevate only after detecting that the target/action requires it. |
| `preferGsudo` | If `gsudo` is available and elevation is required, use it before other elevation methods. |
| `requireElevated` | The action must run elevated or fail before doing work. |

Default policy should be `whenRequired` with `preferGsudo` enabled when `gsudo` is discovered.

## gsudo

If `gsudo` is available, prefer it for launching the elevated helper process. This can reduce repeated UAC prompts because `gsudo` can reuse an elevated session according to its own configuration.

Discovery:

```text
1. Look for gsudo.exe in PATH.
2. If not found, check common install paths.
3. Record version and path in diagnostics.
```

Do not assume `gsudo` is installed, trusted, or already elevated. Tool results must report which path was used.

## When elevation is required

Elevation may be needed when:

- the target process is elevated and the helper is not
- UI Automation cannot inspect the target due to integrity level
- message delivery or physical input helpers need to run at matching integrity
- installation, system settings, or administrator tools are involved

Elevation is not a substitute for user confirmation on risky actions. It only changes process privileges.

## Result contract

Every action that may need elevation should include:

```json
{
  "elevation": {
    "policy": "preferGsudo",
    "required": true,
    "used": true,
    "method": "gsudo",
    "gsudoPath": "C:\\Program Files\\gsudo\\Current\\gsudo.exe",
    "reason": "Target process is elevated and current helper is not.",
    "helperIntegrity": "high",
    "targetIntegrity": "high"
  }
}
```

If elevation is required but not available:

```json
{
  "ok": false,
  "error": {
    "code": "elevation_required",
    "message": "Target window belongs to an elevated process.",
    "remediation": "Run the helper elevated or install/configure gsudo."
  },
  "elevation": {
    "policy": "whenRequired",
    "required": true,
    "used": false,
    "method": "none"
  }
}
```

## Safety

- Do not run all actions elevated by default.
- Do not silently elevate destructive actions.
- Log the elevation method and target process.
- Re-check target identity after elevation; HWNDs and process state can change.
- If the target is on the secure desktop, return a clear unsupported or requires-user-interaction result.
