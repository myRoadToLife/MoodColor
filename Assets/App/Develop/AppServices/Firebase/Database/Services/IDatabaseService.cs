using System.Threading.Tasks;
using System.Collections.Generic;
using App.Develop.AppServices.Firebase.Database.Models;
using Firebase.Database;

namespace App.Develop.AppServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс базового сервиса для работы с Firebase Realtime Database
    /// </summary>
    public interface IDatabaseService : IEmotionDatabaseService, IUserProfileDatabaseService
    {
        /// <summary>
        /// Ссылка на корень базы данных
        /// </summary>
        DatabaseReference RootReference { get; }
        
        /// <summary>
        /// ID текущего пользователя
        /// </summary>
        string UserId { get; }
        
        /// <summary>
        /// Проверяет, аутентифицирован ли пользователь
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Менеджер пакетных операций
        /// </summary>
        FirebaseBatchManager BatchManager { get; }
        
        /// <summary>
        /// Обновляет ID пользователя для работы с базой данных
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        void UpdateUserId(string userId);
        
        /// <summary>
        /// Обновляет текущую эмоцию пользователя
        /// </summary>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="intensity">Интенсивность эмоции</param>
        Task UpdateCurrentEmotion(string emotionType, float intensity);
        
        /// <summary>
        /// Обновляет статусы синхронизации нескольких записей одним батчем
        /// </summary>
        Task UpdateEmotionSyncStatusBatch(Dictionary<string, SyncStatus> recordStatusPairs);
        
        /// <summary>
        /// Удаляет несколько записей из истории одним батчем
        /// </summary>
        Task DeleteEmotionHistoryRecordBatch(List<string> recordIds);
        
        /// <summary>
        /// Создает резервную копию данных пользователя
        /// </summary>
        Task<string> CreateBackup();
        
        /// <summary>
        /// Восстанавливает данные пользователя из резервной копии
        /// </summary>
        Task<bool> RestoreFromBackup(string backupId);
        
        /// <summary>
        /// Получает список доступных резервных копий
        /// </summary>
        Task<string[]> GetAvailableBackups();
        
        /// <summary>
        /// Проверяет подключение к базе данных
        /// </summary>
        /// <returns>True если подключение установлено, иначе False</returns>
        Task<bool> CheckConnection();
    }
} 