using System;
using System.Collections.Generic;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database.Models
{
    /// <summary>
    /// Модель данных активной сессии пользователя
    /// </summary>
    public class ActiveSessionData
    {
        /// <summary>
        /// Уникальный идентификатор устройства
        /// </summary>
        public string DeviceId { get; set; }
        
        /// <summary>
        /// Информация об устройстве (модель, ОС и т.д.)
        /// </summary>
        public string DeviceInfo { get; set; }
        
        /// <summary>
        /// IP-адрес устройства (может быть недоступен)
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// Время последней активности (в миллисекундах с начала эпохи)
        /// </summary>
        public long LastActivityTimestamp { get; set; }
        
        /// <summary>
        /// Преобразует объект в словарь для Firebase
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["deviceId"] = DeviceId ?? "",
                ["deviceInfo"] = DeviceInfo ?? "",
                ["ipAddress"] = IpAddress ?? "",
                ["lastActivityTimestamp"] = LastActivityTimestamp
            };
        }
        
        /// <summary>
        /// Создает объект сессии с данными текущего устройства
        /// </summary>
        public static ActiveSessionData CreateFromCurrentDevice()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            
            // Проверяем, что deviceId не пустой
            if (string.IsNullOrEmpty(deviceId))
            {
                // Используем запасной вариант, если deviceUniqueIdentifier недоступен
                deviceId = GenerateDeviceId();
                MyLogger.LogWarning($"⚠️ SystemInfo.deviceUniqueIdentifier вернул пустое значение, используем сгенерированный ID: {deviceId}", MyLogger.LogCategory.Firebase);
            }
            
            return new ActiveSessionData
            {
                DeviceId = deviceId,
                DeviceInfo = $"{SystemInfo.deviceModel}, {SystemInfo.operatingSystem}",
                IpAddress = "unknown", // В Unity сложно получить IP клиента
                LastActivityTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        
        /// <summary>
        /// Генерирует более надежный идентификатор устройства
        /// </summary>
        private static string GenerateDeviceId()
        {
            // Объединяем различные характеристики устройства для большей надежности
            string combinedInfo = $"{SystemInfo.deviceModel}_{SystemInfo.deviceName}_{SystemInfo.operatingSystem}_{SystemInfo.processorType}";
            
            // Если доступны, добавляем более специфичные идентификаторы
            if (!string.IsNullOrEmpty(SystemInfo.deviceModel))
                combinedInfo += "_" + SystemInfo.deviceModel;
                
            // Создаем хеш из комбинированной информации
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(combinedInfo);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                // Преобразуем хеш в строку
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Возвращает текущий deviceId
        /// </summary>
        public static string GetCurrentDeviceId()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            
            MyLogger.Log($"🔍 [DEVICE-ID] Получен DeviceId из SystemInfo: '{deviceId}' (длина: {deviceId?.Length ?? 0})", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"🔍 [DEVICE-ID] Информация об устройстве: Model='{SystemInfo.deviceModel}', Name='{SystemInfo.deviceName}', OS='{SystemInfo.operatingSystem}'", MyLogger.LogCategory.Firebase);
            
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = GenerateDeviceId();
                MyLogger.Log($"🔍 [DEVICE-ID] Сгенерирован новый DeviceId: '{deviceId}' (длина: {deviceId.Length})", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.Log($"🔍 [DEVICE-ID] Используем DeviceId из SystemInfo: '{deviceId}' (длина: {deviceId.Length})", MyLogger.LogCategory.Firebase);
            }
            
            return deviceId;
        }
    }
} 