using System.Text.Json;
using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services;

public class SettingsService
{
    private readonly ILocalStorage _storage;
    private const string Key = "app-settings";
    public AppSettings Settings { get; private set; } = new();
    public event Action? Changed;
    private static SettingsService? _instance;
    // Setter is public for testability
    public static SettingsService? Instance {
        get => _instance;
        set => _instance = value;
    }
    private bool _systemPrefersDark;
    // Setter is public for testability
    public bool SystemPrefersDark {
        get => _systemPrefersDark;
        set => _systemPrefersDark = value;
    }

    public SettingsService(ILocalStorage storage)
    {
        _storage = storage;
        Load();
        Instance = this;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Settings);
        _storage.SetItem(Key, json);
        Changed?.Invoke();
    }

    private void Load()
    {
        var json = _storage.GetItem(Key);
        if (!string.IsNullOrWhiteSpace(json))
        {
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded != null) Settings = loaded;
        }
    }

    public void NotifyChanged() => Changed?.Invoke();

    public ThemeMode EffectiveTheme => Settings.Theme switch
    {
        ThemeMode.System => (SystemPrefersDark ? ThemeMode.Dark : ThemeMode.Light),
        _ => Settings.Theme
    };

    [Microsoft.JSInterop.JSInvokable("UpdateSystemTheme")] public static void UpdateSystemTheme(bool dark)
    {
        if (Instance != null)
        {
            Instance.SystemPrefersDark = dark;
            Instance.Changed?.Invoke();
        }
    }
}