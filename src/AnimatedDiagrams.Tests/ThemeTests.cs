using Xunit;
using AnimatedDiagrams.Models;
using AnimatedDiagrams.Services;

namespace AnimatedDiagrams.Tests;

public class ThemeTests
{
    public class DummyStorage : ILocalStorage
    {
        public string? Item;
        public string? GetItem(string key) => Item;
        public void SetItem(string key, string value) => Item = value;
    }

    [Fact]
    public void EffectiveTheme_ExplicitLight()
    {
        var svc = new SettingsService(new DummyStorage());
        svc.Settings.Theme = ThemeMode.Light;
        svc.SystemPrefersDark = true;
        Assert.Equal(ThemeMode.Light, svc.EffectiveTheme);
    }

    [Fact]
    public void EffectiveTheme_ExplicitDark()
    {
        var svc = new SettingsService(new DummyStorage());
        svc.Settings.Theme = ThemeMode.Dark;
        svc.SystemPrefersDark = false;
        Assert.Equal(ThemeMode.Dark, svc.EffectiveTheme);
    }

    [Fact]
    public void EffectiveTheme_SystemPrefersDark()
    {
        var svc = new SettingsService(new DummyStorage());
        svc.Settings.Theme = ThemeMode.System;
        svc.SystemPrefersDark = true;
        Assert.Equal(ThemeMode.Dark, svc.EffectiveTheme);
    }

    [Fact]
    public void EffectiveTheme_SystemPrefersLight()
    {
        var svc = new SettingsService(new DummyStorage());
        svc.Settings.Theme = ThemeMode.System;
        svc.SystemPrefersDark = false;
        Assert.Equal(ThemeMode.Light, svc.EffectiveTheme);
    }

    [Fact]
    public void UpdateSystemTheme_ChangesEffectiveTheme()
    {
        var svc = new SettingsService(new DummyStorage());
        SettingsService.Instance = svc;
        svc.Settings.Theme = ThemeMode.System;
        svc.SystemPrefersDark = false;
        Assert.Equal(ThemeMode.Light, svc.EffectiveTheme);
        SettingsService.UpdateSystemTheme(true);
        Assert.Equal(ThemeMode.Dark, svc.EffectiveTheme);
        SettingsService.UpdateSystemTheme(false);
        Assert.Equal(ThemeMode.Light, svc.EffectiveTheme);
    }
}
