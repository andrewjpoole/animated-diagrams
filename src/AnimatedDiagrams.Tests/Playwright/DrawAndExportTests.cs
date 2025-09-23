
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace AnimatedDiagrams.Tests.Playwright;

public class DrawAndExportTests : PlaywrightTestBase
{


    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    [Fact]
    public async Task DrawPath_HasUniqueId_AndStrokeLinejoinRound()
    {
        // Wait for core UI (mode button) to ensure Blazor render complete
        await _page.Locator("button:has-text(\"Mode:\")").First.WaitForAsync();
        var canvasSelector = ".canvas-wrapper";
        await _page.WaitForSelectorAsync(canvasSelector);

        async Task PerformDrawAsync()
        {
            var box = await _page.Locator("svg.diagram-canvas").BoundingBoxAsync();
            Assert.NotNull(box);
            var startX = box!.X + box.Width * 0.25;
            var startY = box.Y + box.Height * 0.5;
            var endX = box.X + box.Width * 0.75;
            var endY = startY;
            await _page.Mouse.MoveAsync((float)startX, (float)startY);
            await _page.Mouse.DownAsync();
            int steps = 15;
            for (int i=1;i<=steps;i++)
            {
                var x = (float)(startX + (endX - startX) * i / steps);
                var y = (float)(startY + (endY - startY) * i / steps);
                await _page.Mouse.MoveAsync(x, y);
                await Task.Delay(15);
            }
            await _page.Mouse.UpAsync();
        }

        await PerformDrawAsync();
        // Wait for path to appear
        for (int i=0;i<10;i++)
        {
            var count = await _page.EvaluateAsync<int>("() => document.querySelectorAll('svg.diagram-canvas path').length");
            if (count > 0) break;
            await Task.Delay(150);
        }

        // Export SVG
        await _page.ClickAsync(".file-controls .sidebar-btn:text('Export')");
        var svg = await _page.EvaluateAsync<string>("localStorage.getItem('lastExportedSvg')");
        Assert.Contains("stroke-linejoin=\"round\"", svg);
        // Extract path ID from PathEditor UI
        var labelText = await _page.InnerTextAsync(".path-editor ul li:first-child");
        var idMatch = System.Text.RegularExpressions.Regex.Match(labelText, "[a-f0-9]{12}");
        Assert.True(idMatch.Success, $"No path ID found in PathEditor label: {labelText}");
        var pathId = idMatch.Value;
        Assert.Matches("^[a-f0-9]{12}$", pathId);
    }

    // ...other test methods...

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
