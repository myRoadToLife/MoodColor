using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using Firebase.Database;

namespace App.Develop.AppServices.Firebase.Database.Interfaces
{
    public interface IDatabaseService : IDisposable
    {
        #region Authentication
        // Текущий ID пользователя
        string UserId { get; }
        
        // Проверка авторизации
        bool IsAuthenticated { get; }
        
        // Установка текущего пользователя
        void SetCurrentUser(string userId);
        #endregion
        
        #region User Profile
        // Получение профиля пользователя
        Task<UserProfile> GetUserProfile(string userId = null);
        
        // Создание профиля нового пользователя
        Task CreateUserProfile(string userId, string email, string nickname = null);
        
        // Обновление профиля пользователя
        Task UpdateUserProfile(UserProfile profile);
        
        // Обновление настроек пользователя
        Task UpdateUserSettings(UserSettings settings);
        
        // Удаление профиля пользователя
        Task DeleteUserProfile();
        #endregion
        
        #region Emotions
        // Получение текущих эмоций пользователя
        Task<Dictionary<string, EmotionData>> GetUserEmotions();
        
        // Обновление текущей эмоции
        Task UpdateCurrentEmotion(string emotionType, float intensity);
        
        // Получение текущей эмоции
        Task<CurrentEmotion> GetCurrentEmotion();
        #endregion
        
        #region Jars
        // Получение банок пользователя
        Task<Dictionary<string, JarData>> GetUserJars();
        
        // Обновление банки
        Task UpdateJar(string jarType, JarData jarData);
        
        // Обновление уровня заполнения банки
        Task UpdateJarAmount(string jarType, int amount);
        #endregion
        
        #region Emotion History
        // Получение истории эмоций
        Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        
        // Получение истории эмоций по типу
        Task<List<EmotionHistoryRecord>> GetEmotionHistoryByType(string emotionType, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        
        // Добавление записи в историю эмоций
        Task AddEmotionHistoryRecord(EmotionHistoryRecord record);
        
        // Добавление записи в историю эмоций на основе эмоции и события
        Task AddEmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType);
        
        // Пакетное добавление записей в историю
        Task AddEmotionHistoryBatch(List<EmotionHistoryRecord> records);
        
        // Получение несинхронизированных записей
        Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionRecords(int limit = 100);
        
        // Обновление статуса синхронизации записи
        Task UpdateEmotionSyncStatus(string recordId, SyncStatus status);
        
        // Удаление записи из истории
        Task DeleteEmotionHistoryRecord(string recordId);
        
        // Получение статистики по эмоциям за период
        Task<Dictionary<string, int>> GetEmotionStatistics(DateTime startDate, DateTime endDate);
        #endregion
        
        #region Sync Settings
        // Получение настроек синхронизации
        Task<EmotionSyncSettings> GetSyncSettings();
        
        // Обновление настроек синхронизации
        Task UpdateSyncSettings(EmotionSyncSettings settings);
        #endregion
        
        #region Listeners
        // Подписка на изменения эмоций
        Task<DatabaseReference> SubscribeToEmotions(EventHandler<EmotionData> callback);
        
        // Подписка на изменения текущей эмоции
        Task<DatabaseReference> SubscribeToCurrentEmotion(EventHandler<CurrentEmotion> callback);
        
        // Подписка на изменения банок
        Task<DatabaseReference> SubscribeToJars(EventHandler<Dictionary<string, JarData>> callback);
        
        // Подписка на изменения настроек синхронизации
        Task<DatabaseReference> SubscribeToSyncSettings(EventHandler<EmotionSyncSettings> callback);
        
        // Отписка от обновлений
        void Unsubscribe(DatabaseReference reference);
        #endregion
        
        #region General
        // Обновление произвольных данных пользователя
        Task UpdateUserData(Dictionary<string, object> updates);
        
        // Создание резервной копии данных пользователя
        Task<string> CreateBackup();
        
        // Восстановление из резервной копии
        Task RestoreFromBackup(string backupId);
        #endregion
    }
} 