namespace AnimatedDiagrams.Models;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public class KeyboardShortcut
{
    public string Action { get; set; } = string.Empty; // e.g. "Undo"
    public string Keys { get; set; } = string.Empty;   // e.g. "Ctrl+Z"
}

public class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public List<string> SavedColors { get; set; } = new();
    public List<KeyboardShortcut> Shortcuts { get; set; } = new();
    public bool ShowDebugOverlay { get; set; } = false;
    public string LightCanvasColor { get; set; } = "#ffffff";
    public string DarkCanvasColor { get; set; } = "#2b2b2b"; // configurable dark background

    // Smoothing strategy for path drawing
    public SmoothingType SmoothingStrategy { get; set; } = SmoothingType.QuadraticBezier;
    // If true, aggressively smooth straight lines on mouseup
    public bool ExtremeAutoSmoothingOfStraightishLines { get; set; } = false;

    // Keybinding for draw mode toggle/cycle (default: F2)
    public string SelectDrawToggleKey { get; set; } = "d";
    // Keybinding for select mode (default: 's')
    public string SelectModeKey { get; set; } = "s";
}

public class RecordingOptions
{
    public int Fps { get; set; } = 30;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Quality { get; set; } = 90; // percent
    public int EndPauseMs { get; set; } = 1000;
    public bool InsertInitialThumbnail { get; set; } = true;
    public int PreAnimationBlankMs { get; set; } = 500;
}