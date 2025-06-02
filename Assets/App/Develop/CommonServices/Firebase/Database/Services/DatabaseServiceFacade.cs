using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Firebase.Database;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Фасад, объединяющий все специализированные сервисы Firebase Database
    /// </summary>
    public class DatabaseServiceFacade : FirebaseDatabaseServiceBase, IDatabaseService
    {
        #region Private Fields
        private readonly UserProfileDatabaseService _profileService;
        private readonly JarDatabaseService _jarService;
        private readonly GameDataDatabaseService _gameDataService;
        private readonly SessionManagementService _sessionService;
        private readonly BackupDatabaseService _backupService;
        private readonly EmotionDatabaseService _emotionService;
        private readonly RegionalDatabaseService _regionalService;
        private bool _isUpdatingChildServices = false; // Флаг для предотвращения рекурсии
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр фасада сервисов базы данных
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        /// <param name="profileService">Сервис профиля пользователя</param>
        /// <param name="jarService">Сервис баночек эмоций</param>
        /// <param name="gameDataService">Сервис игровых данных</param>
        /// <param name="sessionService">Сервис управления сессиями</param>
        /// <param name="backupService">Сервис резервных копий</param>
        /// <param name="emotionService">Сервис эмоций</param>
        /// <param name="regionalService">Сервис региональных данных</param>
        public DatabaseServiceFacade(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService,
            UserProfileDatabaseService profileService,
            JarDatabaseService jarService,
            GameDataDatabaseService gameDataService,
            SessionManagementService sessionService,
            BackupDatabaseService backupService,
            EmotionDatabaseService emotionService,
            RegionalDatabaseService regionalService)
            : base(database, cacheManager, validationService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _jarService = jarService ?? throw new ArgumentNullException(nameof(jarService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _emotionService = emotionService ?? throw new ArgumentNullException(nameof(emotionService));
            _regionalService = regionalService ?? throw new ArgumentNullException(nameof(regionalService));
            
            // Отключаем взаимные подписки от дочерних сервисов, чтобы предотвратить циклические вызовы
            _profileService.UserIdChanged -= UpdateChildServicesUserId;
            _jarService.UserIdChanged -= UpdateChildServicesUserId;
            _gameDataService.UserIdChanged -= UpdateChildServicesUserId;
            _sessionService.UserIdChanged -= UpdateChildServicesUserId;
            _backupService.UserIdChanged -= UpdateChildServicesUserId;
            _emotionService.UserIdChanged -= UpdateChildServicesUserId;
            _regionalService.UserIdChanged -= UpdateChildServicesUserId;
            
            // Передаем событие изменения ID пользователя всем сервисам
            UserIdChanged += UpdateChildServicesUserId;
            
            MyLogger.Log("✅ DatabaseServiceFacade инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        // Переопределяем метод UpdateUserId, чтобы избежать циклической рекурсии
        public override void UpdateUserId(string userId)
        {
            // Вызываем базовую реализацию, которая обновит _userId и вызовет событие UserIdChanged
            base.UpdateUserId(userId);
        }

        // Обработчик события UserIdChanged, который обновляет все дочерние сервисы
        private void UpdateChildServicesUserId(string userId)
        {
            // Если уже идет обновление дочерних сервисов, выходим, чтобы избежать рекурсии
            if (_isUpdatingChildServices)
            {
                return;
            }

            try
            {
                _isUpdatingChildServices = true;
                
                // Обновляем userId во всех дочерних сервисах
                _profileService.UpdateUserId(userId);
                _jarService.UpdateUserId(userId);
                _gameDataService.UpdateUserId(userId);
                _sessionService.UpdateUserId(userId);
                _backupService.UpdateUserId(userId);
                _emotionService.UpdateUserId(userId);
                _regionalService.UpdateUserId(userId);
                
                MyLogger.Log($"✅ UserId обновлен во всех дочерних сервисах: {(string.IsNullOrEmpty(userId) ? "null" : userId.Substring(0, Math.Min(8, userId.Length)) + "...")}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении UserId в дочерних сервисах: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
            finally
            {
                _isUpdatingChildServices = false;
            }
        }

        #region IDatabaseService Implementation

        #region EmotionDatabaseService Methods
        public async Task<Dictionary<string, EmotionData>> GetUserEmotions()
        {
            return await _emotionService.GetUserEmotions();
        }

        public async Task UpdateUserEmotions(Dictionary<string, EmotionData> emotions)
        {
            await _emotionService.UpdateUserEmotions(emotions);
        }

        public async Task UpdateUserEmotion(EmotionData emotion)
        {
            await _emotionService.UpdateUserEmotion(emotion);
        }

        public async Task AddEmotionHistoryRecord(EmotionHistoryRecord record)
        {
            await _emotionService.AddEmotionHistoryRecord(record);
        }

        public async Task AddEmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType)
        {
            await _emotionService.AddEmotionHistoryRecord(emotion, eventType);
        }

        public async Task AddEmotionHistoryBatch(List<EmotionHistoryRecord> records)
        {
            await _emotionService.AddEmotionHistoryBatch(records);
        }

        public async Task UpdateEmotionSyncStatusBatch(Dictionary<string, SyncStatus> recordStatusPairs)
        {
            await _emotionService.UpdateEmotionSyncStatusBatch(recordStatusPairs);
        }

        public async Task DeleteEmotionHistoryRecordBatch(List<string> recordIds)
        {
            await _emotionService.DeleteEmotionHistoryRecordBatch(recordIds);
        }

        public async Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            return await _emotionService.GetEmotionHistory(startDate, endDate, limit);
        }

        public async Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionHistory(int limit = 50)
        {
            return await _emotionService.GetUnsyncedEmotionHistory(limit);
        }

        public async Task UpdateEmotionHistoryRecordStatus(string recordId, SyncStatus status)
        {
            await _emotionService.UpdateEmotionHistoryRecordStatus(recordId, status);
        }

        public async Task DeleteEmotionHistoryRecord(string recordId)
        {
            await _emotionService.DeleteEmotionHistoryRecord(recordId);
        }

        public async Task<Dictionary<string, int>> GetEmotionStatistics(DateTime startDate, DateTime endDate)
        {
            return await _emotionService.GetEmotionStatistics(startDate, endDate);
        }

        public async Task<EmotionSyncSettings> GetSyncSettings()
        {
            return await _emotionService.GetSyncSettings();
        }

        public async Task UpdateSyncSettings(EmotionSyncSettings settings)
        {
            await _emotionService.UpdateSyncSettings(settings);
        }

        public async Task ClearEmotionHistory()
        {
            await _emotionService.ClearEmotionHistory();
        }

        public async Task<bool> SaveEmotionHistoryRecord(EmotionHistoryRecord record)
        {
            // Делегируем вызов в _emotionService, если там есть подходящий метод
            // В противном случае, реализуем здесь
            if (record == null)
            {
                MyLogger.LogError("❌ Запись истории эмоций не может быть пустой", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                await _emotionService.AddEmotionHistoryRecord(record);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения записи истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        #endregion

        #region UserProfileDatabaseService Methods
        public async Task<UserProfile> GetUserProfile(string userId = null)
        {
            return await _profileService.GetUserProfile(userId);
        }

        public async Task CreateUserProfile(UserProfile profile, string userId = null)
        {
            await _profileService.CreateUserProfile(profile, userId);
        }

        public async Task UpdateUserProfile(UserProfile profile, string userId = null)
        {
            await _profileService.UpdateUserProfile(profile, userId);
        }

        public async Task UpdateUserProfileField(string field, object value, string userId = null)
        {
            await _profileService.UpdateUserProfileField(field, value, userId);
        }

        public async Task<bool> UserProfileExists(string userId = null)
        {
            return await _profileService.UserProfileExists(userId);
        }

        public async Task<bool> NicknameExists(string nickname)
        {
            return await _profileService.NicknameExists(nickname);
        }

        public async Task<(bool available, string error)> CheckNicknameAvailability(string nickname)
        {
            return await _profileService.CheckNicknameAvailability(nickname);
        }
        #endregion

        #region JarDatabaseService Methods
        public async Task<Dictionary<string, JarData>> GetUserJars()
        {
            return await _jarService.GetUserJars();
        }

        public async Task UpdateJar(string emotionType, JarData jar)
        {
            await _jarService.UpdateJar(emotionType, jar);
        }

        public async Task UpdateJarAmount(string emotionType, int amountToAdd)
        {
            await _jarService.UpdateJarAmount(emotionType, amountToAdd);
        }

        public async Task UpdateJarLevel(string emotionType, int level)
        {
            await _jarService.UpdateJarLevel(emotionType, level);
        }

        public async Task UpdateJarCustomization(string emotionType, JarCustomization customization)
        {
            await _jarService.UpdateJarCustomization(emotionType, customization);
        }
        #endregion

        #region GameDataDatabaseService Methods
        public async Task SaveUserGameData(GameData gameData)
        {
            await _gameDataService.SaveUserGameData(gameData);
        }

        public async Task<GameData> LoadUserGameData()
        {
            return await _gameDataService.LoadUserGameData();
        }
        #endregion

        #region SessionManagementService Methods
        public async Task<Dictionary<string, ActiveSessionData>> GetActiveSessions()
        {
            return await _sessionService.GetActiveSessions();
        }

        public async Task<bool> RegisterActiveSession()
        {
            return await _sessionService.RegisterActiveSession();
        }

        public async Task<bool> ClearActiveSessions()
        {
            return await _sessionService.ClearActiveSessions();
        }

        public async Task<bool> ClearActiveSession(string deviceId)
        {
            return await _sessionService.ClearActiveSession(deviceId);
        }

        public async Task<bool> CheckActiveSessionExists(string currentDeviceId)
        {
            return await _sessionService.CheckActiveSessionExists(currentDeviceId);
        }

        public async Task<bool> CheckUserExists(string userId)
        {
            return await _sessionService.CheckUserExists(userId);
        }

        public async Task<bool> UpdateActiveSession(string deviceId)
        {
            return await _sessionService.UpdateActiveSession(deviceId);
        }
        #endregion

        #region BackupDatabaseService Methods
        public async Task<string> CreateBackup()
        {
            return await _backupService.CreateBackup();
        }

        public async Task<bool> RestoreFromBackup(string backupId)
        {
            return await _backupService.RestoreFromBackup(backupId);
        }

        public async Task<string[]> GetAvailableBackups()
        {
            return await _backupService.GetAvailableBackups();
        }
        #endregion

        #region RegionalDatabaseService Methods
        public async Task<Dictionary<string, RegionData>> GetAllRegionData()
        {
            return await _regionalService.GetAllRegionData();
        }

        public async Task<RegionData> GetRegionData(string regionName)
        {
            return await _regionalService.GetRegionData(regionName);
        }

        public async Task<bool> SaveRegionData(string regionName, RegionData regionData)
        {
            return await _regionalService.SaveRegionData(regionName, regionData);
        }

        public async Task<bool> UpdateRegionEmotionStats(string regionName, string emotionType, int count)
        {
            return await _regionalService.UpdateRegionEmotionStats(regionName, emotionType, count);
        }

        public async Task<bool> IncrementRegionEmotionCount(string regionName, string emotionType, int increment = 1)
        {
            return await _regionalService.IncrementRegionEmotionCount(regionName, emotionType, increment);
        }

        public async Task<bool> DeleteRegionData(string regionName)
        {
            return await _regionalService.DeleteRegionData(regionName);
        }

        public async Task<List<string>> GetAvailableRegions()
        {
            return await _regionalService.GetAvailableRegions();
        }
        #endregion

        #endregion

        #region Other methods
        public async Task UpdateCurrentEmotion(string emotionType, float intensity)
        {
            // Создаем объект эмоции
            var emotion = new EmotionData
            {
                Id = emotionType,
                Type = emotionType,
                Intensity = intensity,
                LastUpdated = DateTime.UtcNow
            };
            
            // Обновляем текущую эмоцию
            await _emotionService.UpdateUserEmotion(emotion);
            
            // Добавляем запись в историю
            await _emotionService.AddEmotionHistoryRecord(emotion, EmotionEventType.ValueChanged);
        }
        
        /// <summary>
        /// Добавляет запись о новой эмоции
        /// </summary>
        /// <param name="emotionData">Данные об эмоции</param>
        public async Task AddEmotion(EmotionData emotionData)
        {
            if (emotionData == null)
            {
                MyLogger.LogError("❌ Данные эмоции не могут быть пустыми", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                // Обновляем текущую эмоцию пользователя
                await UpdateUserEmotion(emotionData);
                
                // Добавляем запись в историю
                await AddEmotionHistoryRecord(emotionData, EmotionEventType.ValueChanged);
                
                MyLogger.Log($"✅ Добавлена запись о эмоции: {emotionData.Type} с интенсивностью {emotionData.Intensity}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при добавлении эмоции: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Добавляет очки в профиль пользователя
        /// </summary>
        /// <param name="points">Количество очков для добавления</param>
        public async Task AddPointsToProfile(int points)
        {
            if (!IsAuthenticated)
            {
                MyLogger.LogWarning("⚠️ Невозможно добавить очки в профиль: пользователь не аутентифицирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                // Получаем текущий профиль
                var profile = await GetUserProfile();
                if (profile == null)
                {
                    MyLogger.LogError("❌ Невозможно добавить очки: профиль пользователя не найден", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Обновляем количество очков
                int currentPoints = profile.TotalPoints;
                int newPoints = currentPoints + points;
                
                // Обновляем значение в базе данных
                await UpdateUserProfileField("totalPoints", newPoints);
                
                MyLogger.Log($"✅ Добавлено {points} очков в профиль пользователя. Было: {currentPoints}, стало: {newPoints}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при добавлении очков в профиль: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        #endregion

        #region Override Methods
        /// <summary>
        /// Освобождает ресурсы сервиса и всех внутренних сервисов
        /// </summary>
        public override void Dispose()
        {
            try
            {
                MyLogger.Log("Disposing DatabaseServiceFacade...", MyLogger.LogCategory.Firebase);
                
                // Отписка от событий происходит в базовом классе
                base.Dispose();
                
                // Освобождаем ресурсы внутренних сервисов
                _profileService?.Dispose();
                _jarService?.Dispose();
                _gameDataService?.Dispose();
                _sessionService?.Dispose();
                _backupService?.Dispose();
                _emotionService?.Dispose();
                _regionalService?.Dispose();
                
                MyLogger.Log("✅ DatabaseServiceFacade: все ресурсы освобождены.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при освобождении ресурсов DatabaseServiceFacade: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion
    }
} 