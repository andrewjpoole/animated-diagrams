using System.Text.Json;
using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services;

public class StyleRuleService
{
    private const string StorageKey = "style-rules";
    private readonly ILocalStorage _localStorage;
    public List<StyleRule> Rules { get; private set; } = new();
    public event Action? Changed;

    public StyleRuleService(ILocalStorage localStorage)
    {
        _localStorage = localStorage;
        Load();
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
            double left = c.Field switch
            {
                StyleConditionField.StrokeWidth => path.StrokeWidth,
                StyleConditionField.Opacity => path.Opacity,
                StyleConditionField.StrokeOpacity => path.Opacity, // placeholder
                StyleConditionField.PathLength => EstimatePathLength(path.D),
                _ => 0
            };
            if (!Compare(left, c.Value, c.Comparison)) return false;
        }
        return true;
    }

    private static bool Compare(double left, double right, StyleComparison cmp) => cmp switch
    {
        StyleComparison.Equal => Math.Abs(left - right) < 0.0001,
        StyleComparison.Less => left < right,
        StyleComparison.Greater => left > right,
        StyleComparison.LessOrEqual => left <= right,
        StyleComparison.GreaterOrEqual => left >= right,
        _ => false
    };

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
                        p.StrokeWidth = a.Value;
                        break;
                    case StyleActionField.Opacity:
                        p.Opacity = a.Value;
                        break;
                    case StyleActionField.StrokeOpacity:
                        p.Opacity = a.Value; // placeholder separate stroke opacity later
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
        Changed?.Invoke();
        return matches;
    }
}

public interface ILocalStorage
{
    string? GetItem(string key);
    void SetItem(string key, string value);
}