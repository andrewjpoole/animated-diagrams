using Xunit;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace AnimatedDiagrams.Tests.Playwright;

public abstract class PlaywrightTestBase : IAsyncLifetime
{
    protected IPlaywright _playwright = default!;
    protected IBrowser _browser = default!;
    protected IPage _page = default!;
    protected Process? _devServer;
    protected string _baseUrl = string.Empty;
    protected IBrowserContext? BrowserContext { get; private set; } = null;

    public virtual async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        BrowserContext = await _browser.NewContextAsync();
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var projectPath = Path.Combine(solutionRoot, "AnimatedDiagrams", "AnimatedDiagrams.csproj");
        int port;
        using (var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        }
        _baseUrl = $"http://127.0.0.1:{port}";
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --project \"{projectPath}\" --urls={_baseUrl}",
            WorkingDirectory = solutionRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        _devServer = Process.Start(psi);
        await Task.Delay(5000); // Wait for server to start
        _page = await BrowserContext.NewPageAsync();
        await _page.GotoAsync(_baseUrl);
    }

    public virtual async Task DisposeAsync()
    {
        if (_devServer != null && !_devServer.HasExited) _devServer.Kill();
        if (_page != null) await _page.CloseAsync();
        if (BrowserContext != null) await BrowserContext.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
