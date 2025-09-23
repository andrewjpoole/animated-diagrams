using Microsoft.JSInterop;

namespace AnimatedDiagrams.Services;

public class BrowserLocalStorage : ILocalStorage, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module;
    private readonly IJSRuntime _js;
    private readonly Dictionary<string, string> _cache = new();

    public BrowserLocalStorage(IJSRuntime js)
    {
        _js = js;
        _module = new(() => js.InvokeAsync<IJSObjectReference>("import", "./localStorageInterop.js").AsTask());
    }

    public string? GetItem(string key)
    {
        // Try cache first, then localStorage via JS interop
        if (_cache.TryGetValue(key, out var v)) {
            return v;
        }
        string? jsValue = null;
        try
        {
            jsValue = GetItemFromJs(key);
        }
        catch (Exception ex) { Console.WriteLine($"[LocalStorage] GetItem error: {ex.Message}"); }
        if (jsValue != null)
        {
            _cache[key] = jsValue;
            return jsValue;
        }
        return null;
    }

    private string? GetItemFromJs(string key)
    {
        if (_js is IJSInProcessRuntime jsInProcess)
        {
            return jsInProcess.Invoke<string>("eval", $"localStorage.getItem('{key}')") ?? null;
        }
        return null;
    }

    public void SetItem(string key, string value)
    {
        _cache[key] = value;
        _ = SetItemInternalAsync(key, value);
    }

    private async Task SetItemInternalAsync(string key, string value)
    {
        try
        {
            var module = await _module.Value;
            await module.InvokeVoidAsync("setItem", key, value);
        }
        catch
        {
            // Fallback: direct JS interop for WASM
            await _js.InvokeVoidAsync("eval", $"localStorage.setItem('{key}', '{value.Replace("'", "\\'")}')");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var m = await _module.Value;
            await m.DisposeAsync();
        }
    }
}