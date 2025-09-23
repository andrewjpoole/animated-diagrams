# Animated Diagrams (Blazor WebAssembly)

Interactive SVG path editor with animation & style rule scaffolding. Built per `specification.md` with drawing, style rules, undo/redo, export, and an end-to-end Playwright integration test.

## Features Implemented
- Freehand path drawing (smoothed quadratic path generation)
- Sidebar panels: Paths, Hints, Style Rules, Settings, Export
- Style rule matching skeleton (conditions + actions)
- Undo / Redo snapshot stack
- SVG export (embeds hints as comments)
- Local storage persistence for settings & style rules
- Debug overlay (toggle in Settings) showing pointer + transform metrics
- Integration test: launches dev server, draws a path, exports SVG, asserts path output, captures screenshot

## Pending / Roadmap
- Path animation playback
- Recording (media export)
- Extended style rule actions
- Enhanced theming & dark/light customization
- Additional integration & unit tests

## Prerequisites
- .NET 8 SDK
- PowerShell (already available on Windows)

## Running the App Manually
```pwsh
cd workspace
# Run with hot reload
dotnet run --project .\AnimatedDiagrams\AnimatedDiagrams.csproj
# App will bind to the ports from launchSettings (e.g. http://localhost:5150)
```
Open the reported URL in your browser.

## Running Playwright Integration Tests
Playwright browsers must be installed once after restoring packages.

1. Build the solution:
```pwsh
dotnet build .\workspace.sln
```
2. Install Playwright browsers (the script is generated into the test project's output folder):
```pwsh
pwsh .\AnimatedDiagrams.Tests\bin\Debug\net8.0\playwright.ps1 install
```
(If you build Release, adjust the path accordingly.)

3. Run tests:
```pwsh
dotnet test .\workspace.sln --logger "trx;LogFileName=test-results.trx"
```

### Artifacts
The integration test produces two files in:
```
AnimatedDiagrams.Tests\artifacts\
```
Files:
- `draw-export-<timestamp>.svg` – exported SVG captured from the app
- `draw-export-<timestamp>.png` – full page screenshot after drawing & export

### Troubleshooting Test Failures
| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| Timeout: Server did not start within timeout | Port collision or build failure | Re-run `dotnet build`, ensure no other process uses chosen dynamic port. |
| Browser launch error | Browsers not installed | Re-run the playwright install script. |
| Screenshot empty / no path | Drawing event failed | Confirm selector `.diagram-canvas` exists; run app manually to verify drawing works. |

**Dynamic Port Logic**: The test now selects a free loopback port at runtime and starts the dev server with `--urls` so concurrent test runs do not collide.

## Debug Overlay
Toggle `Show Debug Overlay` in Settings to display:
- Pointer (client + transformed) coordinates
- Zoom level
- Canvas bounding rect & offsets
- Current path point counts

Use this to validate cursor alignment when adjusting transforms.

## Export Hook (for Tests)
On export the SVG string is assigned to `window.__lastExportedSvg`. This is used only by automated tests; do not rely on it for production scenarios.

## Security & Hardening Notes
- Some geometry uses `eval`-style JS interop fallbacks for rapid iteration; replace with strongly-typed `IJSRuntime` calls before production.
- No untrusted input is currently processed beyond local user gestures.

## Next Steps (Suggested)
- Implement stroke animation timeline controls
- Add multi-path selection & bulk style application
- Improve SVG import (file picker + parse existing paths)
- Add deterministic unit tests for path smoothing function

---
Feel free to extend this README with deeper architectural notes as features evolve.
