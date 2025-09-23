namespace AnimatedDiagrams.Models;

public enum StyleConditionField
{
    StrokeWidth,
    Opacity,
    StrokeOpacity,
    PathLength
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
    AnimationSpeed
}

public class StyleRuleCondition
{
    public StyleConditionField Field { get; set; }
    public StyleComparison Comparison { get; set; }
    public double Value { get; set; }
}

public class StyleRuleAction
{
    public StyleActionField Field { get; set; }
    public double Value { get; set; }
    public string? Extra { get; set; } // for glow color or multi-value
}

public class StyleRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "New Rule";
    public List<StyleRuleCondition> Conditions { get; set; } = new();
    public List<StyleRuleAction> Actions { get; set; } = new();
}