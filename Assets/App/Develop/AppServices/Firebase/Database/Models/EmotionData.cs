// ИСПРАВЛЕННЫЙ класс EmotionData
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Database.Models
{
    [Serializable]
    public class EmotionData
    {
        // ---> ДОБАВЛЕНО СВОЙСТВО ID <---
        [JsonProperty("id")] // Атрибут для сериализации в JSON с нужным именем
        public string Id { get; set; }
        // ---> КОНЕЦ ДОБАВЛЕНИЯ <---

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("intensity")]
        public int Intensity { get; set; }

        [JsonProperty("colorHex")]
        public string ColorHex { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonIgnore]
        public Color UnityColor
        {
            get
            {
                // Проверка на null или пустую строку перед парсингом
                if (string.IsNullOrEmpty(ColorHex)) return Color.clear;
                ColorUtility.TryParseHtmlString(ColorHex, out Color color);
                return color;
            }
            set => ColorHex = "#" + ColorUtility.ToHtmlStringRGBA(value);
        }

        // Опционально: Конструктор для удобства
        public EmotionData() { }

        public EmotionData(string type, int intensity, string note = null, string colorHex = null, string regionId = null)
        {
            Id = Guid.NewGuid().ToString(); // Генерируем ID при создании
            Type = type;
            Intensity = intensity;
            Note = note;
            ColorHex = colorHex;
            RegionId = regionId;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
