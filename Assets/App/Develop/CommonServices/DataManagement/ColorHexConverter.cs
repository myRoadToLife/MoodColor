using System;
using Newtonsoft.Json;
using UnityEngine;

public class ColorHexConverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        Color32 color32 = value;
        string hex = $"#{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
        writer.WriteValue(hex);
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        string hex = (string)reader.Value;
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;

        Debug.LogWarning($"Invalid color hex: {hex}. Returning white.");
        return Color.white;
    }
}
