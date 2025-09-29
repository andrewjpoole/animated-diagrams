namespace AnimatedDiagrams.Models;

public enum StyleConditionField
{
    StrokeWidth,
    Opacity,
    StrokeOpacity,
    PathLength,
    StrokeLineCap,
    LineType,
    StrokeLineJoin
}

public enum StyleComparison
{
    Equal,
    Less,
    Greater,
    LessOrEqual,
    GreaterOrEqual
}

public enum StyleActionField
{
    StrokeWidth,
    Opacity,
    StrokeOpacity,
    GlowEffect,
    AnimationSpeed,
    StrokeLineCap,
    LineType,
    StrokeLineJoin
}


public class StyleRuleCondition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public StyleConditionField Field { get; set; }
    public StyleComparison Comparison { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class StyleRuleAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public StyleActionField Field { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Extra { get; set; } // for glow color or multi-value
}

public class StyleRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "New Rule";
    public List<StyleRuleCondition> Conditions { get; set; } = new();
    public List<StyleRuleAction> Actions { get; set; } = new();
}