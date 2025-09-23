namespace AnimatedDiagrams.Models;

public class EmptyPathProperties : PathProperties
{
    public string Stroke { get; set; } = "-";
    public double StrokeWidth { get; set; } = double.NaN;
    public double Opacity { get; set; } = double.NaN;
    public string LineType { get; set; } = "-";
    public string StrokeLineCap { get; set; } = "-";
}