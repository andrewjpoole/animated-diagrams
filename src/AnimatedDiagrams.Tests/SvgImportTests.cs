using System;
using System.IO;
using AnimatedDiagrams.Models;
using AnimatedDiagrams.Services;
using Xunit;

namespace AnimatedDiagrams.Tests;

public class SvgImportTests
{
    [Fact]
    public void Import_ConceptsSvg_LoadsPathsAndCircles()
    {
        var pensService = new PensService();
        var editor = new PathEditorState(pensService);
        var service = new SvgFileService(editor);
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var svgPath = Path.Combine(projectDir, "testData", "concepts.svg");
        Assert.True(File.Exists(svgPath), $"SVG file not found: {svgPath}");
        var xml = File.ReadAllText(svgPath);
        service.ImportSvg(xml);
        // At least one path or circle should be loaded
        Assert.Contains(editor.Items, i => i is SvgPathItem || i is SvgCircleItem);
        // Optionally, check for expected count or specific attributes
        Assert.True(editor.Items.Count > 0, "No SVG items loaded from concepts.svg");
    }
}
