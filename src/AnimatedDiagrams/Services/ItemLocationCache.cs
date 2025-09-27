using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services
{
    /// <summary>
    /// Efficiently buckets items by location for fast selection and hit testing.
    /// </summary>
    public class ItemLocationCache
    {
        private readonly int _gridX;
        private readonly int _gridY;
        private readonly double _canvasWidth;
        private readonly double _canvasHeight;
        // Each bucket holds a set of item IDs
        private readonly Dictionary<(int, int), HashSet<string>> _buckets = new();
        // Map item ID to its bucket(s)
        private readonly Dictionary<string, List<(int, int)>> _itemBuckets = new();

        /// <summary>
        /// Bulk-add all items to the cache efficiently (clears previous state).
        /// </summary>
        public void BuildBulk(IEnumerable<PathItem> items)
        {
            _buckets.Clear();
            _itemBuckets.Clear();
            foreach (var item in items)
            {
                var buckets = GetBucketsForItem(item);
                _itemBuckets[item.Id] = buckets;
                foreach (var bucket in buckets)
                {
                    if (!_buckets.TryGetValue(bucket, out var set))
                    {
                        set = new HashSet<string>();
                        _buckets[bucket] = set;
                    }
                    set.Add(item.Id);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the ItemLocationCache class.
        /// <paramref name="gridX"/> Number of buckets in the X direction.
        /// <paramref name="gridY"/> Number of buckets in the Y direction.
        /// <paramref name="canvasWidth"/> Width of the canvas.
        /// <paramref name="canvasHeight"/> Height of the canvas.
        /// </summary>
        public ItemLocationCache(int gridX, int gridY, double canvasWidth, double canvasHeight)
        {
            _gridX = gridX;
            _gridY = gridY;
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
        }

        /// <summary>
        /// Add or update an item in the cache.
        /// </summary>
        public void AddOrUpdate(PathItem item)
        {
            Remove(item.Id);
            var buckets = GetBucketsForItem(item);
            _itemBuckets[item.Id] = buckets;
            foreach (var bucket in buckets)
            {
                if (!_buckets.TryGetValue(bucket, out var set))
                {
                    set = new HashSet<string>();
                    _buckets[bucket] = set;
                }
                set.Add(item.Id);
            }
        }

        /// <summary>
        /// Remove an item from the cache.
        /// </summary>
        public void Remove(string itemId)
        {
            if (_itemBuckets.TryGetValue(itemId, out var buckets))
            {
                foreach (var bucket in buckets)
                {
                    if (_buckets.TryGetValue(bucket, out var set))
                    {
                        set.Remove(itemId);
                    }
                }
                _itemBuckets.Remove(itemId);
            }
        }

        /// <summary>
        /// Get all item IDs in the bucket containing (x, y).
        /// </summary>
        public IEnumerable<string> GetItemsAt(double x, double y)
        {
            var bucket = GetBucketForPoint(x, y);
            if (_buckets.TryGetValue(bucket, out var set))
                return set;
            return Array.Empty<string>();
        }

        /// <summary>
        /// Get all item IDs in buckets overlapping the given rectangle.
        /// </summary>
        public IEnumerable<string> GetItemsInRect(double x, double y, double w, double h)
        {
            var buckets = GetBucketsForRect(x, y, w, h);
            var result = new HashSet<string>();
            foreach (var bucket in buckets)
            {
                if (_buckets.TryGetValue(bucket, out var set))
                {
                    result.UnionWith(set);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a formatted string listing all buckets and their associated item IDs.
        /// </summary>
        public string GetBucketsDebugString()
        {
            var lines = new List<string>();
            foreach (var kvp in _buckets)
            {
                var bucket = kvp.Key;
                var ids = string.Join(", ", kvp.Value);
                lines.Add($"Bucket ({bucket.Item1},{bucket.Item2}): [{ids}]");
            }
            return string.Join("\n", lines);
        }

        private (int, int) GetBucketForPoint(double x, double y)
        {
            int bx = Math.Clamp((int)(x / _canvasWidth * _gridX), 0, _gridX - 1);
            int by = Math.Clamp((int)(y / _canvasHeight * _gridY), 0, _gridY - 1);
            return (bx, by);
        }

        private List<(int, int)> GetBucketsForRect(double x, double y, double w, double h)
        {
            var buckets = new List<(int, int)>();
            int bx0 = Math.Clamp((int)(x / _canvasWidth * _gridX), 0, _gridX - 1);
            int by0 = Math.Clamp((int)(y / _canvasHeight * _gridY), 0, _gridY - 1);
            int bx1 = Math.Clamp((int)((x + w) / _canvasWidth * _gridX), 0, _gridX - 1);
            int by1 = Math.Clamp((int)((y + h) / _canvasHeight * _gridY), 0, _gridY - 1);
            for (int bx = bx0; bx <= bx1; bx++)
                for (int by = by0; by <= by1; by++)
                    buckets.Add((bx, by));
            return buckets;
        }


        /// <summary>
        /// Get all buckets and their item IDs (for debugging).
        /// </summary>
        public Dictionary<(int, int), List<string>> GetAllBucketsWithItems()
        {
            var result = new Dictionary<(int, int), List<string>>();
            foreach (var kvp in _buckets)
            {
                result[kvp.Key] = kvp.Value.ToList();
            }
            return result;
        }

        private List<(int, int)> GetBucketsForItem(PathItem item)
        {
            var buckets = new List<(int, int)>();
            switch (item)
            {
                case SvgCircleItem c:
                    buckets = GetBucketsForRect(c.Cx - c.R, c.Cy - c.R, c.R * 2, c.R * 2);
                    break;
                case SvgPathItem p:
                    var bounds = p.Bounds ?? GetPathBounds(p.D);
                    buckets = GetBucketsForRect(bounds.x, bounds.y, bounds.w, bounds.h);
                    break;
            }
            return buckets;
        }

        private (double x, double y, double w, double h) GetPathBounds(string d)
        {
            // Simple bounding box estimation for path data
            var tokens = d.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (double.TryParse(tokens[i], out var x))
                {
                    if (i + 1 < tokens.Length && double.TryParse(tokens[i + 1], out var y))
                    {
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                        i++;
                    }
                }
            }
            if (minX == double.MaxValue) minX = minY = maxX = maxY = 0;
            return (minX, minY, maxX - minX, maxY - minY);
        }
    }
}
