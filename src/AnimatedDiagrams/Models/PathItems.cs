namespace AnimatedDiagrams.Models;

public enum PathItemType
{
    Path,
    Circle,
    PauseHint,
    SpeedHint
}

public abstract class PathItem
{
    public string Id { get; init; } = GenerateId();
    public PathItemType ItemType { get; protected set; }
    public bool Selected { get; set; }
    public bool Highlight { get; set; }

    private static string GenerateId()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 12).ToLower();
    }
}

public class SvgPathItem : PathItem, PathProperties
{
    public string D { get; set; } = string.Empty; // path data
    public string Stroke { get; set; } = "#000";
    public double StrokeWidth { get; set; } = 2;
    public double Opacity { get; set; } = 1.0;
    public string LineType { get; set; } = "solid";
    public string StrokeLineCap { get; set; } = "round";
    public string StrokeLineJoin { get; set; } = "round";    
    public SvgPathItem() { ItemType = PathItemType.Path; }
}

public class SvgCircleItem : PathItem
{
    public double Cx { get; set; }
    public double Cy { get; set; }
    public double R { get; set; }
    public string Fill { get; set; } = "#000";
    public double Opacity { get; set; } = 1.0;
    public SvgCircleItem() { ItemType = PathItemType.Circle; }
}

public class PauseHintItem : PathItem
{
    public int Milliseconds { get; set; } = 500; // default pause
    public PauseHintItem() { ItemType = PathItemType.PauseHint; }
}

public class SpeedHintItem : PathItem
{
    public double Multiplier { get; set; } = 1.0; // relative speed multiplier
    public SpeedHintItem() { ItemType = PathItemType.SpeedHint; }
}