using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using System.Threading.Tasks;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с эмоциями в Firebase Database
    /// </summary>
    public interface IEmotionDatabaseService
    {
        /// <summary>
        /// Получает все эмоции пользователя
        /// </summary>
        Task<Dictionary<string, EmotionData>> GetUserEmotions();
        
        /// <summary>
        /// Обновляет эмоции пользователя
        /// </summary>
        Task UpdateUserEmotions(Dictionary<string, EmotionData> emotions);
        
        /// <summary>
        /// Обновляет конкретную эмоцию пользователя
        /// </summary>
        Task UpdateUserEmotion(EmotionData emotion);
        
        /// <summary>
        /// Добавляет запись в историю эмоций
        /// </summary>
        Task AddEmotionHistoryRecord(EmotionHistoryRecord record);
        
        /// <summary>
        /// Добавляет запись в историю эмоций на основе эмоции и события
        /// </summary>
        Task AddEmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType);
        
        /// <summary>
        /// Пакетное добавление записей в историю
        /// </summary>
        Task AddEmotionHistoryBatch(List<EmotionHistoryRecord> records);
        
        /// <summary>
        /// Обновляет статусы синхронизации нескольких записей одним батчем
        /// </summary>
        Task UpdateEmotionSyncStatusBatch(Dictionary<string, SyncStatus> recordStatusPairs);
        
        /// <summary>
        /// Удаляет несколько записей из истории одним батчем
        /// </summary>
        Task DeleteEmotionHistoryRecordBatch(List<string> recordIds);
        
        /// <summary>
        /// Получает историю эмоций пользователя
        /// </summary>
        Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        
        /// <summary>
        /// Получает несинхронизированные записи истории эмоций
        /// </summary>
        Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionHistory(int limit = 50);
        
        /// <summary>
        /// Обновляет статус синхронизации записи истории эмоций
        /// </summary>
        Task UpdateEmotionHistoryRecordStatus(string recordId, SyncStatus status);
        
        /// <summary>
        /// Удаляет запись из истории эмоций
        /// </summary>
        Task DeleteEmotionHistoryRecord(string recordId);
        
        /// <summary>
        /// Получает статистику по эмоциям за период
        /// </summary>
        Task<Dictionary<string, int>> GetEmotionStatistics(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Получает настройки синхронизации пользователя
        /// </summary>
        Task<EmotionSyncSettings> GetSyncSettings();
        
        /// <summary>
        /// Обновляет настройки синхронизации пользователя
        /// </summary>
        Task UpdateSyncSettings(EmotionSyncSettings settings);
        
        /// <summary>
        /// Очищает историю эмоций пользователя
        /// </summary>
        Task ClearEmotionHistory();
    }
} 