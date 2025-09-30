using System.Collections.Generic;
namespace AnimatedDiagrams.Models
{
    public class EditorSnapshot
    {
        public List<PathItem> Items { get; set; } = new();
        public string? Operation { get; set; }
    }
}