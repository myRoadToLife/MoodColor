using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement.DataProviders;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Compression
{
    /// <summary>
    /// Класс для сжатия данных эмоций перед отправкой в Firebase
    /// </summary>
    public class EmotionDataCompression
    {
        #region Константы
        
        private const int MinSizeForCompression = 1024; // Минимальный размер для сжатия (1KB)
        private const string CompressedPrefix = "COMP:";
        
        #endregion
        
        #region Публичные методы

        /// <summary>
        /// Сжимает данные эмоции для экономии трафика
        /// </summary>
        /// <param name="emotionData">Данные эмоции для сжатия</param>
        /// <returns>Сжатая строка в формате Base64</returns>
        public static string CompressEmotionData(EmotionData emotionData)
        {
            if (emotionData == null)
                return null;
                
            string json = JsonConvert.SerializeObject(emotionData, 
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                
            // Сжимаем только если размер превышает минимальный порог
            if (json.Length < MinSizeForCompression)
                return json;
                
            return CompressedPrefix + CompressString(json);
        }
        
        /// <summary>
        /// Сжимает список записей истории эмоций для экономии трафика
        /// </summary>
        /// <param name="records">Список записей для сжатия</param>
        /// <returns>Сжатая строка в формате Base64</returns>
        public static string CompressEmotionHistory(List<EmotionHistoryRecord> records)
        {
            if (records == null || records.Count == 0)
                return null;
                
            string json = JsonConvert.SerializeObject(records, 
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                
            // Сжимаем только если размер превышает минимальный порог
            if (json.Length < MinSizeForCompression)
                return json;
                
            return CompressedPrefix + CompressString(json);
        }
        
        /// <summary>
        /// Распаковывает данные эмоции
        /// </summary>
        /// <param name="compressedData">Сжатые данные в формате Base64 или JSON</param>
        /// <returns>Объект EmotionData</returns>
        public static EmotionData DecompressEmotionData(string compressedData)
        {
            if (string.IsNullOrEmpty(compressedData))
                return null;
                
            string json;
            
            if (compressedData.StartsWith(CompressedPrefix))
            {
                // Удаляем префикс и распаковываем
                string base64 = compressedData.Substring(CompressedPrefix.Length);
                json = DecompressString(base64);
            }
            else
            {
                // Данные не сжаты
                json = compressedData;
            }
            
            try
            {
                return JsonConvert.DeserializeObject<EmotionData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при распаковке данных эмоции: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Распаковывает список записей истории эмоций
        /// </summary>
        /// <param name="compressedData">Сжатые данные в формате Base64 или JSON</param>
        /// <returns>Список объектов EmotionHistoryRecord</returns>
        public static List<EmotionHistoryRecord> DecompressEmotionHistory(string compressedData)
        {
            if (string.IsNullOrEmpty(compressedData))
                return new List<EmotionHistoryRecord>();
                
            string json;
            
            if (compressedData.StartsWith(CompressedPrefix))
            {
                // Удаляем префикс и распаковываем
                string base64 = compressedData.Substring(CompressedPrefix.Length);
                json = DecompressString(base64);
            }
            else
            {
                // Данные не сжаты
                json = compressedData;
            }
            
            try
            {
                return JsonConvert.DeserializeObject<List<EmotionHistoryRecord>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при распаковке истории эмоций: {ex.Message}");
                return new List<EmotionHistoryRecord>();
            }
        }

        /// <summary>
        /// Оптимизирует запись эмоции, удаляя ненужные данные перед отправкой
        /// </summary>
        /// <param name="record">Запись для оптимизации</param>
        /// <returns>Оптимизированная запись</returns>
        public static EmotionHistoryRecord OptimizeEmotionRecord(EmotionHistoryRecord record)
        {
            if (record == null)
                return null;
            
            // Создаем копию для оптимизации
            var optimized = new EmotionHistoryRecord
            {
                Id = record.Id,
                Type = record.Type,
                Value = record.Value,
                Intensity = record.Intensity,
                Timestamp = record.Timestamp,
                EventType = record.EventType,
                SyncStatus = record.SyncStatus
            };
            
            // Добавляем опциональные поля только если они не пустые
            if (!string.IsNullOrEmpty(record.ColorHex))
                optimized.ColorHex = record.ColorHex;
                
            if (!string.IsNullOrEmpty(record.Note))
                optimized.Note = record.Note;
                
            if (!string.IsNullOrEmpty(record.RegionId))
                optimized.RegionId = record.RegionId;
                
            if (!string.IsNullOrEmpty(record.LocalId))
                optimized.LocalId = record.LocalId;
                
            if (record.Latitude.HasValue && record.Longitude.HasValue)
            {
                optimized.Latitude = record.Latitude;
                optimized.Longitude = record.Longitude;
            }
            
            if (record.Tags != null && record.Tags.Count > 0)
                optimized.Tags = new List<string>(record.Tags);
                
            return optimized;
        }
        
        #endregion
        
        #region Приватные методы
        
        /// <summary>
        /// Сжимает строку с помощью GZip и кодирует в Base64
        /// </summary>
        private static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, System.IO.Compression.CompressionMode.Compress, true))
                {
                    gzip.Write(buffer, 0, buffer.Length);
                }
                
                memory.Position = 0;
                byte[] compressed = new byte[memory.Length];
                memory.Read(compressed, 0, compressed.Length);
                
                return Convert.ToBase64String(compressed);
            }
        }
        
        /// <summary>
        /// Распаковывает строку из Base64 с помощью GZip
        /// </summary>
        private static string DecompressString(string base64)
        {
            byte[] gzBuffer = Convert.FromBase64String(base64);
            
            using (MemoryStream memory = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                memory.Write(gzBuffer, 4, gzBuffer.Length - 4);
                
                byte[] buffer = new byte[msgLength];
                
                memory.Position = 0;
                using (GZipStream gzip = new GZipStream(memory, System.IO.Compression.CompressionMode.Decompress))
                {
                    gzip.Read(buffer, 0, buffer.Length);
                }
                
                return Encoding.UTF8.GetString(buffer);
            }
        }
        
        #endregion
    }
} 