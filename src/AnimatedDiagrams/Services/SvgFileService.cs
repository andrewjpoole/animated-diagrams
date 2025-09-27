using System.Text;
using System.Xml.Linq;
using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services;

public class SvgFileService
{
    private readonly PathEditorState _editor;
    private static SvgFileService? _lastInstance;
    public SvgFileService(PathEditorState editor)
    {
        _editor = editor;
        _lastInstance = this;
    }

    public string ExportSvg()
    {
        // Attempt to derive bounds; if parsing fails fall back to default canvas size (2000x1200 from viewBox)
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;

        void ConsiderPoint(double x, double y)
        {
            if (x < minX) minX = x; if (y < minY) minY = y; if (x > maxX) maxX = x; if (y > maxY) maxY = y;
        }

        foreach (var item in _editor.Items)
        {
            switch (item)
            {
                case SvgPathItem p:
                    // Very naive parse: look for numeric tokens in the path D string
                    var tokens = p.D.Replace("M"," ").Replace("Q"," ").Replace("L"," ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i=0;i+1<tokens.Length;i+=2)
                    {
                        if (double.TryParse(tokens[i], out var px) && double.TryParse(tokens[i+1], out var py))
                        {
                            ConsiderPoint(px, py);
                        }
                    }
                    break;
                case SvgCircleItem c:
                    ConsiderPoint(c.Cx - c.R, c.Cy - c.R);
                    ConsiderPoint(c.Cx + c.R, c.Cy + c.R);
                    break;
            }
        }

        // Fallback if no drawable geometry
        if (double.IsInfinity(minX) || double.IsInfinity(minY))
        {
            minX = 0; minY = 0; maxX = 2000; maxY = 1200; // default viewBox
        }

        var width = maxX - minX;
        var height = maxY - minY;
        if (width <= 0) width = 1; if (height <= 0) height = 1;

        // Add whitespace margin
        double margin = Math.Max(width, height) * 0.05 + 20; // 5% of largest dimension + 20px minimum
        minX -= margin;
        minY -= margin;
        width += margin * 2;
        height += margin * 2;

        XNamespace svgNs = "http://www.w3.org/2000/svg";
        XNamespace xlinkNs = "http://www.w3.org/1999/xlink";
        var svg = new XElement(svgNs + "svg",
            new XAttribute(XNamespace.Xmlns + "xlink", xlinkNs),
            new XAttribute("version", "1.1"),
            new XAttribute("width", width.ToString("0.###")),
            new XAttribute("height", height.ToString("0.###")),
            new XAttribute("viewBox", $"{minX:0.###} {minY:0.###} {width:0.###} {height:0.###}")
        );

        foreach (var item in _editor.Items)
        {
            switch (item)
            {
                case PauseHintItem p:
                    svg.Add(new XComment($"Pause:{p.Milliseconds}"));
                    break;
                case SpeedHintItem s:
                    svg.Add(new XComment($"Speed:{s.Multiplier}"));
                    break;
                case SvgPathItem path:
                    svg.Add(new XElement(svgNs + "path",
                        new XAttribute("d", path.D),
                        new XAttribute("stroke", path.Stroke),
                        new XAttribute("stroke-width", path.StrokeWidth),
                        new XAttribute("fill", "none"),
                        new XAttribute("opacity", path.Opacity),
                        new XAttribute("stroke-linecap", path.StrokeLineCap),
                        new XAttribute("stroke-linejoin", path.StrokeLineJoin),
                        new XAttribute("id", path.Id),
                        path.LineType == "dashed" ? new XAttribute("stroke-dasharray", "10 6") :
                        path.LineType == "dotted" ? new XAttribute("stroke-dasharray", "3 7") : null));
                    break;
                case SvgCircleItem c:
                    svg.Add(new XElement(svgNs + "circle",
                        new XAttribute("cx", c.Cx),
                        new XAttribute("cy", c.Cy),
                        new XAttribute("r", c.R),
                        new XAttribute("fill", c.Fill),
                        new XAttribute("opacity", c.Opacity)));
                    break;
            }
        }

        var doc = new XDocument(new XDeclaration("1.0","UTF-8","yes"), svg);
        var result = doc.ToString(SaveOptions.DisableFormatting);
        var xmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
        if (!result.TrimStart().StartsWith("<?xml"))
        {
            result = xmlHeader + result;
        }
        if (string.IsNullOrWhiteSpace(result))
        {
            result = xmlHeader + "<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"1\" height=\"1\" viewBox=\"0 0 1 1\" />";
        }
        return result;
    }

    public void ImportSvg(string xml)
    {
        // Move SVG parsing and geometry to background thread, then update UI
        System.Threading.Tasks.Task.Run(() =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var doc = XDocument.Parse(xml);
            var items = new List<PathItem>();
            var comments = new List<XComment>();
            void ImportElement(XNode node)
            {
                switch (node)
                {
                    case XComment comment:
                        comments.Add(comment);
                        break;
                    case XElement el:
                        if (el.Name.LocalName == "path")
                        {
                            var idAttr = el.Attribute("id")?.Value;
                            var d = el.Attribute("d")?.Value ?? string.Empty;
                            var bounds = AnimatedDiagrams.PathGeometry.Path.GetBounds(d);
                            var path = new SvgPathItem
                            {
                                D = d,
                                Stroke = el.Attribute("stroke")?.Value ?? "#000",
                                StrokeWidth = double.TryParse(el.Attribute("stroke-width")?.Value, out var sw) ? sw : 2,
                                Opacity = double.TryParse(el.Attribute("opacity")?.Value, out var op) ? op : 1.0,
                                Id = !string.IsNullOrWhiteSpace(idAttr) ? idAttr! : Guid.NewGuid().ToString(),
                                Bounds = bounds
                            };
                            items.Add(path);
                        }
                        else if (el.Name.LocalName == "circle")
                        {
                            var idAttr = el.Attribute("id")?.Value;
                            var cx = double.TryParse(el.Attribute("cx")?.Value, out var cxVal) ? cxVal : 0;
                            var cy = double.TryParse(el.Attribute("cy")?.Value, out var cyVal) ? cyVal : 0;
                            var r = double.TryParse(el.Attribute("r")?.Value, out var rVal) ? rVal : 0;
                            var circ = new SvgCircleItem
                            {
                                Cx = cx,
                                Cy = cy,
                                R = r,
                                Fill = el.Attribute("fill")?.Value ?? "#000",
                                Opacity = double.TryParse(el.Attribute("opacity")?.Value, out var cop) ? cop : 1.0,
                                Id = !string.IsNullOrWhiteSpace(idAttr) ? idAttr! : Guid.NewGuid().ToString()
                            };
                            items.Add(circ);
                        }
                        foreach (var child in el.Nodes()) ImportElement(child);
                        break;
                }
            }
            ImportElement(doc.Root!);
            // Compute bounds for zoom/center
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var item in items)
            {
                switch (item)
                {
                    case SvgPathItem p when p.Bounds.HasValue:
                        var b = p.Bounds.Value;
                        minX = Math.Min(minX, b.x);
                        minY = Math.Min(minY, b.y);
                        maxX = Math.Max(maxX, b.x + b.w);
                        maxY = Math.Max(maxY, b.y + b.h);
                        break;
                    case SvgCircleItem c:
                        minX = Math.Min(minX, c.Cx - c.R);
                        minY = Math.Min(minY, c.Cy - c.R);
                        maxX = Math.Max(maxX, c.Cx + c.R);
                        maxY = Math.Max(maxY, c.Cy + c.R);
                        break;
                }
            }
            // Zoom and center in 2000x1200 canvas
            double canvasW = 2000, canvasH = 1200;
            stopwatch.Stop();
            Console.WriteLine($"[SVG Import] Loaded {items.Count} items in {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
            double contentW = Math.Max(1, maxX - minX);
            double contentH = Math.Max(1, maxY - minY);
            double margin = 40; // pixels
            double zoomX = (canvasW - margin * 2) / contentW;
            double zoomY = (canvasH - margin * 2) / contentH;
            double zoom = Math.Min(zoomX, zoomY);
            zoom = Math.Min(zoom, 10.0); // Clamp to max zoom
            zoom = Math.Max(zoom, 0.1); // Clamp to min zoom
            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;
            double offsetX = canvasW / 2.0 - cx * zoom;
            double offsetY = canvasH / 2.0 - cy * zoom;

            // Dynamically determine grid size for cache
            int pathCount = items.Count(i => i is SvgPathItem || i is SvgCircleItem);
            int grid = (int)Math.Clamp(Math.Ceiling(Math.Sqrt(pathCount)), 3, 10);
            Console.WriteLine($"[SVG Import] Using {grid}x{grid} cache buckets for {pathCount} items");

            // Now update UI/editor on main thread
            System.Threading.Tasks.Task.Run(() =>
            {
                _editor.New();
                _editor.InitLocationCache(grid, grid, canvasW, canvasH);
                _editor.Items.AddRange(items);
                _editor.LocationCache?.BuildBulk(items);
                foreach (var comment in comments) ParseComment(comment.Value);
                if (_editor.Items.Count > 0)
                {
                    _editor.SetViewport(zoom, offsetX, offsetY);
                }
                _editor.MarkSaved();
            });
        });
    }

    private void ParseComment(string value)
    {
        if (value.StartsWith("Pause:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(value.Substring(6), out var ms))
            {
                _editor.Add(new PauseHintItem { Milliseconds = ms });
            }
        }
        else if (value.StartsWith("Speed:", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(value.Substring(6), out var mult))
            {
                _editor.Add(new SpeedHintItem { Multiplier = mult });
            }
        }
    }

    [Microsoft.JSInterop.JSInvokable("TestForceExport")] 
    public static string TestForceExport()
    {
        return _lastInstance?.ExportSvg() ?? string.Empty;
    }
}