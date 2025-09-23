namespace AnimatedDiagrams.Models;

public interface PathProperties
{
    string Stroke { get; set; }
    double StrokeWidth { get; set; }
    double Opacity { get; set; }
    string LineType { get; set; }
    string StrokeLineCap { get; set; }
}