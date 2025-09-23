namespace AnimatedDiagrams.Models;

public class PenModel : PathProperties
{
    public Guid PenId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Pen";
    public string Stroke { get; set; } = "#000000";
    public double StrokeWidth { get; set; } = 3;
    public double Opacity { get; set; } = 1.0;
    public string LineType { get; set; } = "solid";
    public string StrokeLineCap { get; set; } = "round";
}
