using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using AnimatedDiagrams.Tests;

namespace AnimatedDiagrams.Tests.Playwright;

public class MultiSelectPlaywrightTests : PlaywrightTestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        // Wait for Blazor UI to be ready
        await _page.WaitForSelectorAsync(".path-editor", new() { Timeout = 10000 });
        // Use Playwright to upload SVG file via file input
        var svgPath = Path.Combine(AppContext.BaseDirectory, "testData", "concepts.svg");
        if (!File.Exists(svgPath))
            throw new FileNotFoundException($"SVG file not found: {svgPath}");
        // Wait for file input to be available
        var fileInput = await _page.QuerySelectorAsync("input[type='file']");
        Assert.NotNull(fileInput);
        await fileInput.SetInputFilesAsync(svgPath);
        // Wait for injected paths to appear in UI
        await _page.WaitForSelectorAsync(".path-editor ul li", new() { Timeout = 10000 });
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public async Task PathEditor_CtrlClick_MultiSelect_Works()
    {
        // Wait for PathEditor to render
    await _page.WaitForSelectorAsync(".path-editor ul li", new() { Timeout = 10000 });
    var firstItem = await _page.QuerySelectorAsync(".path-editor ul li:nth-child(1)");
    var secondItem = await _page.QuerySelectorAsync(".path-editor ul li:nth-child(2)");
    Assert.True(firstItem != null, "First path item not found");
    Assert.True(secondItem != null, "Second path item not found");
    await _page.Keyboard.DownAsync("Control");
    await firstItem.ClickAsync();
    await secondItem.ClickAsync();
    await _page.Keyboard.UpAsync("Control");
    // Wait for selection class
    await _page.WaitForFunctionAsync(@"() => document.querySelectorAll('.path-editor ul li.sel').length >= 2", null, new() { Timeout = 5000 });
    var firstClass = await firstItem.GetAttributeAsync("class");
    var secondClass = await secondItem.GetAttributeAsync("class");
    Assert.Contains("sel", firstClass ?? "");
    Assert.Contains("sel", secondClass ?? "");
    }

    [Fact]
    public async Task PathEditor_ShiftClick_ContiguousSelect_Works()
    {
        await _page.WaitForSelectorAsync(".path-editor ul li", new() { Timeout = 10000 });
        var firstItem = await _page.QuerySelectorAsync(".path-editor ul li:nth-child(1)");
        var thirdItem = await _page.QuerySelectorAsync(".path-editor ul li:nth-child(3)");
        Assert.True(firstItem != null, "First path item not found");
        Assert.True(thirdItem != null, "Third path item not found");
        await firstItem.ClickAsync();
        await _page.Keyboard.DownAsync("Shift");
        await thirdItem.ClickAsync();
        await _page.Keyboard.UpAsync("Shift");
        // Wait for selection class
        await _page.WaitForFunctionAsync(@"() => document.querySelectorAll('.path-editor ul li.sel').length >= 3", null, new() { Timeout = 5000 });
        for (int i = 1; i <= 3; i++)
        {
            var item = await _page.QuerySelectorAsync($".path-editor ul li:nth-child({i})");
            Assert.True(item != null, $"Path item {i} not found");
            var cls = await item.GetAttributeAsync("class");
            Assert.Contains("sel", cls ?? "");
        }
    }

    [Fact]
    public async Task CanvasView_CtrlClick_MultiSelect_Works()
    {
    await BrowserContext!.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
    await _page.WaitForSelectorAsync("svg.diagram-canvas path", new() { Timeout = 10000 });
    var paths = await _page.QuerySelectorAllAsync("svg.diagram-canvas path");
    Assert.True(paths.Count >= 2, "Less than 2 canvas paths found");
    
    // Focus canvas wrapper before sending F2 keybinding
    var canvasWrapper = await _page.QuerySelectorAsync(".canvas-wrapper");
    if (canvasWrapper != null)
        await canvasWrapper.FocusAsync();
    await _page.Keyboard.PressAsync("F2");
    // Get first node of each path (robust regex for negative/decimal coordinates)
    string getFirstNode = @"
        Array.from(document.querySelectorAll('svg.diagram-canvas path')).map(p => {
            let d = p.getAttribute('d');
            let m = d.match(/([Mm])\s*(-?\d*\.?\d+)\s*,?\s*(-?\d*\.?\d+)/);
            return m ? {x:parseFloat(m[2]),y:parseFloat(m[3]), raw: d} : {x: null, y: null, raw: d};
        })
    ";
    var nodes = await _page.EvaluateAsync<dynamic>(getFirstNode);
    if (nodes[0].x == null || nodes[1].x == null)
    {
        var dAttrs = string.Join(" | ", new[] { nodes[0].raw, nodes[1].raw });
        throw new Exception($"Could not parse first node of paths. d attributes: {dAttrs}");
    }
    await _page.Keyboard.DownAsync("Control");
    // Click first node of first path
    await _page.Mouse.MoveAsync((float)nodes[0].x, (float)nodes[0].y);
    await _page.Mouse.DownAsync();
    await _page.Mouse.UpAsync();
    await Task.Delay(200);
    // Click first node of second path
    await _page.Mouse.MoveAsync((float)nodes[1].x, (float)nodes[1].y);
    await _page.Mouse.DownAsync();
    await _page.Mouse.UpAsync();
    await Task.Delay(200);
    await _page.Keyboard.UpAsync("Control");
    // Take screenshot after Ctrl+Up
    await _page.ScreenshotAsync(new() { Path = "CanvasView_CtrlClick_MultiSelect_Works.png" });
    // Wait for selected class
    await _page.WaitForFunctionAsync(@"() => document.querySelectorAll('svg.diagram-canvas path.selected').length >= 2", null, new() { Timeout = 10000 });
    var selectedCount = await _page.EvaluateAsync<int>("document.querySelectorAll('svg.diagram-canvas path.selected').length");
    var debug = await _page.EvaluateAsync<string>("Array.from(document.querySelectorAll('svg.diagram-canvas path')).map(p => p.getAttribute('class')).join(',')");
    Debug.WriteLine($"Canvas path classes: {debug}");
    Assert.True(selectedCount >= 2, $"Less than 2 selected canvas paths. Classes: {debug}");
    await BrowserContext!.Tracing.StopAsync(new() { Path = "CanvasView_CtrlClick_MultiSelect_Works.zip" });
    }

    [Fact]
    public async Task CanvasView_ShiftClick_ContiguousSelect_Works()
    {
    await BrowserContext!.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
    await _page.WaitForSelectorAsync("svg.diagram-canvas path", new() { Timeout = 10000 });
    var paths = await _page.QuerySelectorAllAsync("svg.diagram-canvas path");
    Assert.True(paths.Count >= 3, "Less than 3 canvas paths found");
    
    // Focus canvas wrapper before sending F2 keybinding
    var canvasWrapper = await _page.QuerySelectorAsync(".canvas-wrapper");
    if (canvasWrapper != null)
        await canvasWrapper.FocusAsync();
    await _page.Keyboard.PressAsync("F2");
    // Get first node of each path (robust regex for negative/decimal coordinates)
    string getFirstNode = @"
        Array.from(document.querySelectorAll('svg.diagram-canvas path')).map(p => {
            let d = p.getAttribute('d');
            let m = d.match(/([Mm])\s*(-?\d*\.?\d+)\s*,?\s*(-?\d*\.?\d+)/);
            return m ? {x:parseFloat(m[2]),y:parseFloat(m[3]), raw: d} : {x: null, y: null, raw: d};
        })
    ";
    var nodes = await _page.EvaluateAsync<dynamic>(getFirstNode);
    if (nodes[0].x == null || nodes[2].x == null)
    {
        var dAttrs = string.Join(" | ", new[] { nodes[0].raw, nodes[2].raw });
        throw new Exception($"Could not parse first node of paths. d attributes: {dAttrs}");
    }
    // Click first node of first path
    await _page.Mouse.MoveAsync((float)nodes[0].x, (float)nodes[0].y);
    await _page.Mouse.DownAsync();
    await _page.Mouse.UpAsync();
    await Task.Delay(200);
    await Task.Delay(300);
    await _page.Keyboard.DownAsync("Shift");
    // Click first node of third path
    await _page.Mouse.MoveAsync((float)nodes[2].x, (float)nodes[2].y);
    await _page.Mouse.DownAsync();
    await _page.Mouse.UpAsync();
    await Task.Delay(200);
    await _page.Keyboard.UpAsync("Shift");
    // Take screenshot after Shift+Up
    await _page.ScreenshotAsync(new() { Path = "CanvasView_ShiftClick_ContiguousSelect_Works.png" });
    // Wait for selected class
    await _page.WaitForFunctionAsync(@"() => document.querySelectorAll('svg.diagram-canvas path.selected').length >= 3", null, new() { Timeout = 10000 });
    var selectedCount = await _page.EvaluateAsync<int>("document.querySelectorAll('svg.diagram-canvas path.selected').length");
    var debug = await _page.EvaluateAsync<string>("Array.from(document.querySelectorAll('svg.diagram-canvas path')).map(p => p.getAttribute('class')).join(',')");
    Debug.WriteLine($"Canvas path classes: {debug}");
    Assert.True(selectedCount >= 3, $"Less than 3 selected canvas paths. Classes: {debug}");
    await BrowserContext!.Tracing.StopAsync(new() { Path = "CanvasView_ShiftClick_ContiguousSelect_Works.zip" });
    }
}
