namespace AnimatedDiagrams.Services;

using System;
using System.Collections.Generic;
using AnimatedDiagrams.Models;
using AnimatedDiagrams.PathGeometry;

public enum EditorMode
{
    Select,
    Drawing,
    Animation
}

public class PathEditorState
{
    // Item location cache for fast selection
    public ItemLocationCache? LocationCache { get; private set; }

    public void InitLocationCache(int gridX, int gridY, double canvasWidth, double canvasHeight)
    {
        LocationCache = new ItemLocationCache(gridX, gridY, canvasWidth, canvasHeight);
        // Populate cache with existing items
        foreach (var item in Items)
            LocationCache.AddOrUpdate(item);
    }
    public string DiagramName { get; set; } = "animated-diagram";
    public PensService PensSvc { get; }
    private SettingsService settingsService;

    public PathEditorState(PensService pensSvc, SettingsService settingsService)
    {
        PensSvc = pensSvc;
        this.settingsService = settingsService;
    }

    public void ResetViewport()
    {
        SetViewport(1.0, 0, 0);
    }

    public void Highlight(PathItem? item)
    {
        ClearHighlights();
        if (item != null) item.Highlight = true;
        Changed?.Invoke();
    }

    public event Action? Changed;
    public List<PathItem> Items { get; } = new();
    public bool IsDirty { get; private set; }
    public List<PathItem> SelectedItems { get; private set; } = new();
    public bool EditingSelectedPaths { get; set; } = false;
    public EditorMode Mode { get; private set; } = EditorMode.Drawing;
    public bool ModeWhichShoudIgnorePointerEvents { get { return Mode == EditorMode.Animation; } }
    public double Zoom { get; private set; } = 1.0;
    public double OffsetX { get; private set; } = 0;
    public double OffsetY { get; private set; } = 0;

    public void SetMode(EditorMode mode)
    {
        Mode = mode;
        switch (mode)
        {
            case EditorMode.Drawing:
                Select(null);
                EditingSelectedPaths = false;
                break;
            case EditorMode.Animation:
                EditingSelectedPaths = false;
                Select(null);
                break;
            case EditorMode.Select:
                // No-op, keep selection
                break;
        }
        Changed?.Invoke();
    }

    public void SetViewport(double zoom, double ox, double oy)
    {
        Zoom = zoom; OffsetX = ox; OffsetY = oy; Changed?.Invoke();
    }

    public void ClearHighlights()
    {
        foreach (var i in Items)
            i.Highlight = false;

        Changed?.Invoke();
    }

    public string Serialize() => System.Text.Json.JsonSerializer.Serialize(Items);

    public void New()
    {
        Items.Clear();
        IsDirty = false;
        SelectedItems.Clear();
        Changed?.Invoke();
    }

    public void Add(PathItem item)
    {
        Items.Add(item);
        if (item is SvgPathItem p)
        {
            p.Bounds = AnimatedDiagrams.PathGeometry.Path.GetBounds(p.D);
        }
        LocationCache?.AddOrUpdate(item);
        MarkDirty();
    }

    public void Add(SvgPathItem item)
    {
        item.Bounds = AnimatedDiagrams.PathGeometry.Path.GetBounds(item.D);
        Items.Add(item);
        LocationCache?.AddOrUpdate(item);
        MarkDirty();
    }

    public void Add(SvgCircleItem item)
    {
        Items.Add(item);
        LocationCache?.AddOrUpdate(item);
        MarkDirty();
    }

    public void InsertBefore(PathItem reference, PathItem newItem)
    {
        var idx = Items.IndexOf(reference);
        if (idx < 0) { Items.Add(newItem); }
        else Items.Insert(idx, newItem);
        MarkDirty();
    }

    public void Delete(PathItem item)
    {
        if (Items.Remove(item))
        {
            LocationCache?.Remove(item.Id);
            MarkDirty();
        }
    }

    public void DeleteSelected()
    {
        if (SelectedItems.Count == 0)
            return;

        foreach (var item in SelectedItems)
            Delete(item);
    }

    public void Move(PathItem item, int delta)
    {
        var index = Items.IndexOf(item);
        if (index < 0) return;
        var newIndex = Math.Clamp(index + delta, 0, Items.Count - 1);
        if (newIndex == index) return;
        Items.RemoveAt(index);
        Items.Insert(newIndex, item);
        MarkDirty();
    }

    public void Select(PathItem? item)
    {
        if (item == null)
        {
            foreach (var i in Items) i.Selected = false;
            SelectedItems.Clear();
            Changed?.Invoke();
            return;
        }
        // Single select
        foreach (var i in Items) i.Selected = false;
        item.Selected = true;
        SelectedItems.Clear();
        SelectedItems.Add(item);
        Changed?.Invoke();

    }

    public void SelectMultiple(IEnumerable<PathItem> items)
    {
        foreach (var i in Items) i.Selected = false;
        SelectedItems.Clear();
        foreach (var item in items)
        {
            item.Selected = true;
            SelectedItems.Add(item);
        }
        Changed?.Invoke();
    }

    public void ToggleEditingSelectedPaths()
    {
        if (SelectedItems.Count == 0)
            EditingSelectedPaths = false;
        else
            EditingSelectedPaths = !EditingSelectedPaths;

        Changed?.Invoke();
    }

    public void MarkSaved()
    {
        IsDirty = false;
        Changed?.Invoke();
    }

    private void MarkDirty()
    {
        IsDirty = true;
        Changed?.Invoke();
    }

    public void ApplyStyleToSelected(string? color, double? strokeWidth, double? opacity, string? lineType, string? strokeLineCap)
    {
        foreach (var item in SelectedItems)
        {
            if (item is SvgPathItem path)
            {
                if (color != null)
                    path.Stroke = color;
                if (strokeWidth != null)
                    path.StrokeWidth = strokeWidth.Value;
                if (opacity != null)
                    path.Opacity = opacity.Value;
                if (lineType != null)
                    path.LineType = lineType;
                if (strokeLineCap != null)
                    path.StrokeLineCap = strokeLineCap;
                // Update bounds and cache if geometry changed
                path.Bounds = AnimatedDiagrams.PathGeometry.Path.GetBounds(path.D);
                LocationCache?.AddOrUpdate(path);
            }
            else if (item is SvgCircleItem circle)
            {
                // Circles do not have Stroke/LineType, but can use StrokeWidth and Opacity
                if (strokeWidth != null)
                    circle.R = strokeWidth.Value / 2.0; // Optionally map stroke width to radius
                if (opacity != null)
                    circle.Opacity = opacity.Value;
                // Optionally, if you want to support color for circles, set Fill
                if (color != null)
                    circle.Fill = color;
                LocationCache?.AddOrUpdate(circle);
            }
        }
        Changed?.Invoke();
    }
    
    public void SimplifySelectedItems()
    {
        foreach (var item in SelectedItems.OfType<SvgPathItem>())
        {
            var points = PathData.ToPoints(item.D);
            // Use the smoothing strategy from system settings
            var strategy = settingsService.Settings.SmoothingStrategy;
            var simplified = SmoothingStrategies.BuildPath(points, strategy);
            // Only update if result is valid (starts with M, has at least one segment, not empty)
            if (!string.IsNullOrWhiteSpace(simplified) && simplified.StartsWith("M ") && simplified.Length > 10)
            {
                item.D = simplified;
                item.Bounds = AnimatedDiagrams.PathGeometry.Path.GetBounds(item.D);
                LocationCache?.AddOrUpdate(item);
            }
            else
            {
                Console.WriteLine($"{strategy} produced an invalid path string");
            }
        }
        Changed?.Invoke();
    }
}