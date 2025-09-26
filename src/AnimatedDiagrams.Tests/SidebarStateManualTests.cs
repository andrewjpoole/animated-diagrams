using System;
using System.IO;
using AnimatedDiagrams.Models;
using AnimatedDiagrams.Services;
using Xunit;
using static AnimatedDiagrams.Tests.ThemeTests;

namespace AnimatedDiagrams.Tests;

public class SidebarStateManualTests
{
    [Fact]
    public void PathEditor_NewButton_ClearsEditorAndCanvas()
    {
        // Arrange
        var pensService = new PensService();
        var settingsService = new SettingsService(new DummyStorage());
        var editor = new PathEditorState(pensService, settingsService);
        var service = new SvgFileService(editor);
        var projectDir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(projectDir, "testData")))
            projectDir = Path.GetDirectoryName(projectDir)!;
        var svgPath = Path.Combine(projectDir, "testData", "concepts.svg");
        var originalXml = File.ReadAllText(svgPath);
        service.ImportSvg(originalXml);
        Assert.True(editor.Items.Count > 0, "Should have imported at least one path");

        // Act: Clear editor (simulate New button)
        editor.Items.Clear();
        editor.ResetViewport();

        // Assert
        Assert.Empty(editor.Items);
        Assert.Equal(1.0, editor.Zoom);
        Assert.Equal(0, editor.OffsetX);
        Assert.Equal(0, editor.OffsetY);
    }
}
