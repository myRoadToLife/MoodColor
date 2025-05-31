using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.Utils.Logging;
using Firebase.Database;
using Newtonsoft.Json;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для управления резервными копиями данных в Firebase Database
    /// </summary>
    public class BackupDatabaseService : FirebaseDatabaseServiceBase, IBackupDatabaseService
    {
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса резервных копий
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public BackupDatabaseService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null) 
            : base(database, cacheManager, validationService)
        {
            MyLogger.Log("✅ BackupDatabaseService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region IBackupDatabaseService Implementation
        /// <summary>
        /// Создает резервную копию данных пользователя
        /// </summary>
        public async Task<string> CreateBackup()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для создания резервной копии", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                // Получаем все данные пользователя
                var snapshot = await _database.Child("users").Child(_userId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    throw new InvalidOperationException("Данные пользователя не найдены");
                }
                
                // Создаем ID для резервной копии
                string backupId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                
                // Сохраняем резервную копию
                await _database.Child("backups").Child(_userId).Child(backupId).SetRawJsonValueAsync(snapshot.GetRawJsonValue());
                
                MyLogger.Log($"Резервная копия создана: {backupId}", MyLogger.LogCategory.Firebase);
                return backupId;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка создания резервной копии: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Восстанавливает данные из резервной копии
        /// </summary>
        public async Task<bool> RestoreFromBackup(string backupId)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для восстановления из резервной копии", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(backupId))
                {
                    throw new ArgumentException("ID резервной копии не может быть пустым", nameof(backupId));
                }
                
                // Получаем резервную копию
                var snapshot = await _database.Child("backups").Child(_userId).Child(backupId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    throw new InvalidOperationException($"Резервная копия {backupId} не найдена");
                }
                
                // Восстанавливаем данные (кроме profile, чтобы не перезаписать текущие данные авторизации)
                var backupData = JsonConvert.DeserializeObject<Dictionary<string, object>>(snapshot.GetRawJsonValue());
                
                // Фильтруем поля, которые не нужно восстанавливать
                if (backupData.ContainsKey("profile"))
                {
                    backupData.Remove("profile");
                }
                
                // Восстанавливаем остальные данные
                await _database.Child("users").Child(_userId).UpdateChildrenAsync(backupData);
                
                MyLogger.Log($"Данные восстановлены из резервной копии {backupId}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка восстановления из резервной копии: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        
        /// <summary>
        /// Получает список доступных резервных копий
        /// </summary>
        public async Task<string[]> GetAvailableBackups()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения списка резервных копий", MyLogger.LogCategory.Firebase);
                return Array.Empty<string>();
            }

            try
            {
                var snapshot = await _database.Child("backups").Child(_userId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    MyLogger.Log("Резервные копии не найдены", MyLogger.LogCategory.Firebase);
                    return Array.Empty<string>();
                }
                
                List<string> backupIds = new List<string>();
                
                foreach (var child in snapshot.Children)
                {
                    backupIds.Add(child.Key);
                }
                
                MyLogger.Log($"Найдено {backupIds.Count} резервных копий", MyLogger.LogCategory.Firebase);
                return backupIds.OrderByDescending(id => id).ToArray(); // Сортируем по убыванию даты
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения списка резервных копий: {ex.Message}", MyLogger.LogCategory.Firebase);
                return Array.Empty<string>();
            }
        }
        #endregion
    }
} 