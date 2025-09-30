using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services
{
    public class PathItemJsonConverter : JsonConverter<PathItem>
    {
        public override PathItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                JsonElement typeProp;
                if (!root.TryGetProperty("ItemType", out typeProp))
                {
                    // Try camelCase
                    if (!root.TryGetProperty("itemType", out typeProp))
                    {
                        throw new JsonException("Missing ItemType");
                    }
                }
                var type = (PathItemType)typeProp.GetInt32();
                switch (type)
                {
                    case PathItemType.Path:
                        return JsonSerializer.Deserialize<SvgPathItem>(root.GetRawText(), options);
                    case PathItemType.Circle:
                        return JsonSerializer.Deserialize<SvgCircleItem>(root.GetRawText(), options);
                    case PathItemType.PauseHint:
                        return JsonSerializer.Deserialize<PauseHintItem>(root.GetRawText(), options);
                    case PathItemType.SpeedHint:
                        return JsonSerializer.Deserialize<SpeedHintItem>(root.GetRawText(), options);
                    default:
                        throw new JsonException($"Unknown PathItemType: {type}");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, PathItem value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case SvgPathItem path:
                    JsonSerializer.Serialize(writer, path, options);
                    break;
                case SvgCircleItem circle:
                    JsonSerializer.Serialize(writer, circle, options);
                    break;
                case PauseHintItem pause:
                    JsonSerializer.Serialize(writer, pause, options);
                    break;
                case SpeedHintItem speed:
                    JsonSerializer.Serialize(writer, speed, options);
                    break;
                default:
                    throw new JsonException($"Unknown PathItem type: {value.GetType()}");
            }
        }
    }
}
