using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OmniSharp.Models;

internal class ZeroBasedIndexConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(int) || objectType == typeof(int?) || objectType == typeof(IEnumerable<int>) || objectType == typeof(int[]);

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return new();
        if (objectType == typeof(IEnumerable<int>))
        {
            IEnumerable<int>? results = serializer.Deserialize<IEnumerable<int>>(reader);
            return results?.Select(x => x - 1) ?? Array.Empty<int>();
        }
        else
        {
            return serializer.Deserialize<int?>(reader) ?? default;
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        if (Configuration.UseZeroBasedIndices)
        {
            serializer.Serialize(writer, value);
            return;
        }
        Type objectType = value.GetType();
        if (objectType == typeof(int[]))
        {
            int[] results = (int[])value;
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = results[i] + 1;
            }
        }
        else if (objectType == typeof(IEnumerable<int>))
        {
            var results = (IEnumerable<int>)value;
            value = results.Select(x => x + 1);
        }
        else if (objectType == typeof(int?))
        {
            int? nullable = (int?)value;
            if (nullable.HasValue)
            {
                nullable = nullable.Value + 1;
            }
            value = nullable;
        }
        else if (objectType == typeof(int))
        {
            int intValue = (int)value;
            value = intValue + 1;
        }
        serializer.Serialize(writer, value);
    }
}
