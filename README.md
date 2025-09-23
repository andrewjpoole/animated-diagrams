
# Animated Diagrams🚀

Create, animate, and export hand-drawn-style SVG diagrams, for presentations. Convey detailed information gradually, while guiding your audiences attention, in your own style and handwriting. Repeatable, editable, tweakable. Nice!😎

[Try it out!](https://forkinthecode.net/animated-diagrams/)

## Features
- Draw diagrams with your own style and handwriting
- Import and export SVG files
- Easily reorder paths
- Animation your diagram as if you were drawing it in real time
- Set pauses or change the drawing speed so you can talk/present while the diagram draws itself
- Export recordings as webm files for import into presentation software, 
    with optional thumbnail at the start so when exporting slides you see the final rendered diagram.
- End-to-end Playwright integration tests

## Prerequisites
- .NET 9 SDK
- PowerShell (Windows)

## Running the App
```pwsh
cd src
dotnet run --project .\AnimatedDiagrams\AnimatedDiagrams.csproj
# Open the reported URL (e.g. http://localhost:5150)
```

## Running Playwright Tests
1. Build:
	```pwsh
	dotnet build .\animated-diagrams.sln
	```
2. Install Playwright browsers:
	```pwsh
	pwsh .\AnimatedDiagrams.Tests\bin\Debug\net9.0\playwright.ps1 install
	```
3. Run tests:
	```pwsh
	dotnet test .\animated-diagrams.sln --logger "trx;LogFileName=test-results.trx"
	```

Artifacts: SVG and PNG files are saved in `AnimatedDiagrams.Tests/artifacts/`.


