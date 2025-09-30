using AnimatedDiagrams.Models;
using System.Text.Json;
using Microsoft.JSInterop;

namespace AnimatedDiagrams.Services;

public class UndoRedoService
{
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly IJSRuntime? _js;
    private const string UndoKey = "animatedDiagrams.undoBuffer";
    private const string RedoKey = "animatedDiagrams.redoBuffer";
    private bool _loaded = false;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new PathItemJsonConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public UndoRedoService() { }
    public UndoRedoService(IJSRuntime js)
    {
        _js = js;
        LoadBuffersFromStorageSync();
    }

    public string SerializeSnapshot(IEnumerable<PathItem> items, string? operation = null)
    {
        var snap = new EditorSnapshot { Items = items.Select(i => i).ToList(), Operation = operation };
        return JsonSerializer.Serialize(snap, _jsonOptions);
    }

    public EditorSnapshot? DeserializeSnapshot(string json)
    {
        try { return JsonSerializer.Deserialize<EditorSnapshot>(json, _jsonOptions); }
        catch { return null; }
    }

    public int UndoCount => _undo.Count;
    public int RedoCount => _redo.Count;
    public string? PeekUndoOperation()
    {
        if (_undo.Count == 0) return null;
        var json = _undo.Peek();
        var snap = DeserializeSnapshot(json);
        return snap?.Operation;
    }
    public string? PeekRedoOperation()
    {
        if (_redo.Count == 0) return null;
        var json = _redo.Peek();
        var snap = DeserializeSnapshot(json);
        return snap?.Operation;
    }
    private int _maxBuffer = 20;
    public int MaxBuffer
    {
        get => _maxBuffer;
        set
        {
            _maxBuffer = value > 0 ? value : 1;
            TrimBuffer();
            SaveBuffersToStorage();
        }
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void PushState(string serialized)
    {
        // Only push if different from last state
        if (_undo.Count == 0 || _undo.Peek() != serialized)
        {
            _undo.Push(serialized);
            while (_undo.Count > _maxBuffer)
                _undo.TryPop(out _);
            _redo.Clear();
            SaveBuffersToStorage();
        }
    }

    public string? Undo(string current)
    {
    if (_undo.Count == 0) return null;
    // Push current state to redo stack
    _redo.Push(current);
    // Pop the last state (current)
    _undo.TryPop(out var _);
    // Return the new top of the stack (previous state), or null if empty
    var result = _undo.Count > 0 ? _undo.Peek() : null;
    SaveBuffersToStorage();
    return result;
    }

    public string? Redo(string current)
    {
    if (_redo.Count == 0) return null;
    // Push the current state to undo stack (so redo can be undone)
    _undo.Push(current);
    // Pop the redo state and return it (this becomes the new current)
    var result = _redo.Pop();
    SaveBuffersToStorage();
    return result;
    }

    private void TrimBuffer()
    {
        while (_undo.Count > _maxBuffer)
            _undo.TryPop(out _);
        while (_redo.Count > _maxBuffer)
            _redo.TryPop(out _);
    }

    private async void SaveBuffersToStorage()
    {
        if (_js == null) return;
        try
        {
            var undoArr = _undo.Reverse().ToArray();
            var redoArr = _redo.Reverse().ToArray();
            await _js.InvokeVoidAsync("localStorageInterop.setItem", UndoKey, JsonSerializer.Serialize(undoArr));
            await _js.InvokeVoidAsync("localStorageInterop.setItem", RedoKey, JsonSerializer.Serialize(redoArr));
        }
        catch { }
    }

    private void LoadBuffersFromStorageSync()
    {
        if (_js == null || _loaded) return;
        _loaded = true;
        try
        {
            var undoJson = _js.InvokeAsync<string>("eval", $"localStorage.getItem('{UndoKey}')").GetAwaiter().GetResult();
            var redoJson = _js.InvokeAsync<string>("eval", $"localStorage.getItem('{RedoKey}')").GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(undoJson))
            {
                var arr = JsonSerializer.Deserialize<string[]>(undoJson);
                if (arr != null) foreach (var s in arr) _undo.Push(s);
            }
            if (!string.IsNullOrWhiteSpace(redoJson))
            {
                var arr = JsonSerializer.Deserialize<string[]>(redoJson);
                if (arr != null) foreach (var s in arr) _redo.Push(s);
            }
        }
        catch { }
    }
}