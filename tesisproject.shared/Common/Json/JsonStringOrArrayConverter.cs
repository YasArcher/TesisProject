using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tesisproject.shared.Common.Json
{
    /// <summary>
    /// Allows reading either:
    /// - a JSON array: careers: [{...}, {...}]
    /// - or a JSON string containing an array: careers: "[{...},{...}]"
    /// </summary>
    public sealed class JsonStringOrArrayConverter<TItem> : JsonConverter<List<TItem>>
    {
        public override List<TItem> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Case 1: careers is an array
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = JsonSerializer.Deserialize<List<TItem>>(ref reader, options);
                return list ?? new List<TItem>();
            }

            // Case 2: careers is a string containing JSON array
            if (reader.TokenType == JsonTokenType.String)
            {
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return new List<TItem>();

                try
                {
                    var list = JsonSerializer.Deserialize<List<TItem>>(raw, options);
                    return list ?? new List<TItem>();
                }
                catch
                {
                    // If the string isn't valid JSON, return empty (or throw if you prefer)
                    return new List<TItem>();
                }
            }

            // Anything else => empty
            if (reader.TokenType == JsonTokenType.Null)
                return new List<TItem>();

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing a JSON array or JSON string-array.");
        }

        public override void Write(Utf8JsonWriter writer, List<TItem> value, JsonSerializerOptions options)
        {
            // Write as normal JSON array
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}