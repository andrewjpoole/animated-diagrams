using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace AnimatedDiagrams.Tests.Playwright;

public class SidebarStatePlaywrightTests : PlaywrightTestBase
{

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public async Task CollapsingAndResizingSidebarSections_UpdatesLocalStorage()
    {
        // Collapse the Paths section
        await _page.ClickAsync(".sidebar-section .section-header:text('Paths')");
        // Resize the Style Rules section
        var styleRulesHandle = await _page.QuerySelectorAsync(".sidebar-section:has(.section-header:text('Style Rules')) .resize-handle");
        var box = await styleRulesHandle.BoundingBoxAsync();
        await _page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2 + 50); // Drag down 50px
        await _page.Mouse.UpAsync();

        // Check localStorage for sidebar state
        var pathsCollapsed = await _page.EvaluateAsync<string>("localStorage.getItem('sidebar_Paths_collapsed')");
        var styleRulesHeight = await _page.EvaluateAsync<string>("localStorage.getItem('sidebar_Style Rules_height')");
        Assert.Equal("true", pathsCollapsed);
        Assert.True(int.TryParse(styleRulesHeight, out var h) && h > 400);
    }

    [Fact]
    public async Task SidebarState_IsRespectedOnReload()
    {
        // Set localStorage manually
        await _page.EvaluateAsync("localStorage.setItem('sidebar_Paths_collapsed', 'true')");
        await _page.EvaluateAsync("localStorage.setItem('sidebar_Style Rules_height', '555')");
        await _page.ReloadAsync();
        // Check that Paths section is collapsed and Style Rules height is respected
        // Wait for sidebar sections to exist after reload
        await _page.WaitForSelectorAsync(".sidebar-section:has(.section-header:text('Paths'))");
        await _page.WaitForSelectorAsync(".sidebar-section:has(.section-header:text('Style Rules'))");
        var pathsSection = await _page.QuerySelectorAsync(".sidebar-section:has(.section-header:text('Paths'))");
        var styleRulesSection = await _page.QuerySelectorAsync(".sidebar-section:has(.section-header:text('Style Rules'))");
        Assert.NotNull(pathsSection);
        Assert.NotNull(styleRulesSection);
        var isCollapsed = await pathsSection.EvaluateAsync<bool>("el => el.classList.contains('collapsed')");
        var styleRulesHeight = await styleRulesSection.EvaluateAsync<string>("el => el.style.height");
        Assert.True(isCollapsed);
        Assert.Contains("555px", styleRulesHeight);
    }
}
