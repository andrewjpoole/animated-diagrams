using System.Text.Json;
using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services;

public class StyleRuleService
{
    private const string StorageKey = "style-rules";
    private readonly ILocalStorage _localStorage;
    private PathEditorState? _editor;
    public List<StyleRule> Rules { get; private set; } = new();
    public event Action? Changed;

    public StyleRuleService(ILocalStorage localStorage)
    {
        _localStorage = localStorage;
        Load();
    }

    public void SetEditor(PathEditorState editor)
    {
        _editor = editor;
    }

    public void Add(StyleRule rule)
    {
        Rules.Add(rule);
        Persist();
    }

    public void Delete(StyleRule rule)
    {
        Rules.Remove(rule);
        Persist();
    }

    public void Duplicate(StyleRule rule)
    {
        var copy = new StyleRule
        {
            Name = rule.Name + " Copy",
            Conditions = rule.Conditions.Select(c => new StyleRuleCondition { Field = c.Field, Comparison = c.Comparison, Value = c.Value }).ToList(),
            Actions = rule.Actions.Select(a => new StyleRuleAction { Field = a.Field, Value = a.Value, Extra = a.Extra }).ToList()
        };
        Rules.Add(copy);
        Persist();
    }
    public void SaveRules() => Persist();

    public IEnumerable<PathItem> GetMatches(IEnumerable<PathItem> items, StyleRule rule)
    {
        return items.Where(i => i is SvgPathItem p && Matches(p, rule));
    }

    private bool Matches(SvgPathItem path, StyleRule rule)
    {
        foreach (var c in rule.Conditions)
        {
            switch (c.Field)
            {
                case StyleConditionField.StrokeWidth:
                    if (!Compare(path.StrokeWidth, c.Value, c.Comparison)) return false;
                    break;
                case StyleConditionField.Opacity:
                    if (!Compare(path.Opacity, c.Value, c.Comparison)) return false;
                    break;
                case StyleConditionField.StrokeOpacity:
                    if (!Compare(path.Opacity, c.Value, c.Comparison)) return false; // placeholder
                    break;
                case StyleConditionField.PathLength:
                    if (!Compare(EstimatePathLength(path.D), c.Value, c.Comparison)) return false;
                    break;
                case StyleConditionField.StrokeLineCap:
                    if (!StringEquals(path.StrokeLineCap, c.Value)) return false;
                    break;
                case StyleConditionField.LineType:
                    if (!StringEquals(path.LineType, c.Value)) return false;
                    break;
                case StyleConditionField.StrokeLineJoin:
                    if (!StringEquals(path.StrokeLineJoin, c.Value)) return false;
                    break;
            }
        }
        return true;
    }
    private static bool StringEquals(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Compare(double left, string rightStr, StyleComparison cmp)
    {
        if (!double.TryParse(rightStr, out var right)) return false;
        return cmp switch
        {
            StyleComparison.Equal => Math.Abs(left - right) < 0.0001,
            StyleComparison.Less => left < right,
            StyleComparison.Greater => left > right,
            StyleComparison.LessOrEqual => left <= right,
            StyleComparison.GreaterOrEqual => left >= right,
            _ => false
        };
    }

    private double EstimatePathLength(string d)
    {
        // naive: count commands for now
        return d.Count(ch => ch is 'L' or 'C' or 'Q' or 'M');
    }

    public void ImportJson(string json)
    {
        var rules = JsonSerializer.Deserialize<List<StyleRule>>(json);
        if (rules != null)
        {
            Rules = rules;
            Persist();
        }
    }

    public string ExportJson() => JsonSerializer.Serialize(Rules);

    private void Load()
    {
        var json = _localStorage.GetItem(StorageKey);
        if (!string.IsNullOrWhiteSpace(json))
        {
            var loaded = JsonSerializer.Deserialize<List<StyleRule>>(json);
            if (loaded != null) Rules = loaded;
        }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(Rules);
        _localStorage.SetItem(StorageKey, json);
        Changed?.Invoke();
    }

    public void Apply(PathItem item)
    {
        if (item is not SvgPathItem p) return;
        foreach (var rule in Rules)
        {
            if (!Matches(p, rule)) continue;
            foreach (var a in rule.Actions)
            {
                switch (a.Field)
                {
                    case StyleActionField.StrokeWidth:
                        if (double.TryParse(a.Value, out var sw)) p.StrokeWidth = sw;
                        break;
                    case StyleActionField.Opacity:
                        if (double.TryParse(a.Value, out var op)) p.Opacity = op;
                        break;
                    case StyleActionField.StrokeOpacity:
                        if (double.TryParse(a.Value, out var sop)) p.Opacity = sop; // placeholder separate stroke opacity later
                        break;
                    case StyleActionField.StrokeLineCap:
                        if (!string.IsNullOrWhiteSpace(a.Value)) p.StrokeLineCap = a.Value;
                        break;
                    case StyleActionField.LineType:
                        if (!string.IsNullOrWhiteSpace(a.Value)) p.LineType = a.Value;
                        break;
                    case StyleActionField.StrokeLineJoin:
                        if (!string.IsNullOrWhiteSpace(a.Value)) p.StrokeLineJoin = a.Value;
                        break;
                    case StyleActionField.AnimationSpeed:
                        // handled during animation
                        break;
                    case StyleActionField.GlowEffect:
                        // would set a class or style attribute externally
                        break;
                }
            }
            p.Highlight = true;
        }
    }

    public IEnumerable<PathItem> HighlightMatches(IEnumerable<PathItem> items, StyleRule rule)
    {
        var matches = GetMatches(items, rule).ToList();
        foreach (var m in matches) m.Highlight = true;
        // If _editor is set, clear selection and select matches
        if (_editor != null)
        {
            _editor.SelectMultiple(matches);
        }
        Changed?.Invoke();
        return matches;
    }
}

public interface ILocalStorage
{
    string? GetItem(string key);
    void SetItem(string key, string value);
}