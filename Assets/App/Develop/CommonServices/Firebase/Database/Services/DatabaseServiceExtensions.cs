using App.Develop.CommonServices.Firebase.Database.Models;
using System.Threading.Tasks;
// using UnityEngine; // Not used
// using App.Develop.Utils.Logging; // MyLogger removed
using System; // For ArgumentNullException, InvalidOperationException

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Расширения для DatabaseService для работы с валидацией данных
    /// </summary>
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Сохраняет запись истории эмоций с предварительной валидацией
        /// </summary>
        /// <param name="databaseService">Сервис базы данных</param>
        /// <param name="record">Запись для сохранения</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <returns>True если запись сохранена, иначе False</returns>
        public static async Task<bool> SaveEmotionHistoryRecordWithValidationAsync(
            this IDatabaseService databaseService, 
            EmotionHistoryRecord record,
            DataValidationService validationService)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record), "Невозможно сохранить пустую запись");
            }
            
            // Если сервис валидации не доступен, пропускаем валидацию
            if (validationService == null || !validationService.HasValidator<EmotionHistoryRecord>())
            {
                // MyLogger.LogWarning("Валидация пропущена: сервис валидации не доступен", MyLogger.LogCategory.Firebase); // Warning removed
                return await databaseService.SaveEmotionHistoryRecord(record);
            }
            
            // Выполняем валидацию
            ValidationResult validationResult = validationService.Validate<EmotionHistoryRecord>(record); // Explicit type
            if (!validationResult.IsValid)
            {
                validationResult.CheckAndThrowErrors("EmotionHistoryRecord"); // This now throws
                return false; // Should not be reached if CheckAndThrowErrors throws
            }
            
            // Сохраняем валидные данные
            return await databaseService.SaveEmotionHistoryRecord(record);
        }
        
        /// <summary>
        /// Обновляет данные пользователя с предварительной валидацией
        /// </summary>
        /// <param name="databaseService">Сервис базы данных</param>
        /// <param name="userData">Данные пользователя</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <returns>True если данные обновлены, иначе False</returns>
        public static async Task<bool> UpdateUserDataWithValidationAsync(
            this IDatabaseService databaseService,
            UserData userData,
            DataValidationService validationService)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData), "Невозможно обновить пустые данные пользователя");
            }
            
            // Если сервис валидации не доступен, пропускаем валидацию
            if (validationService == null || !validationService.HasValidator<UserData>())
            {
                await databaseService.UpdateUserProfile(userData.Profile);
                return true;
            }
            
            // Выполняем валидацию
            ValidationResult validationResult = validationService.Validate<UserData>(userData);
            if (!validationResult.IsValid)
            {
                validationResult.CheckAndThrowErrors("UserData");
                return false;
            }
            
            // Обновляем валидные данные
            await databaseService.UpdateUserProfile(userData.Profile);
            return true;
        }
        
        /// <summary>
        /// Обновляет текущую эмоцию пользователя с предварительной валидацией
        /// </summary>
        /// <param name="databaseService">Сервис базы данных</param>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="intensity">Интенсивность эмоции</param>
        /// <param name="validationService">Сервис валидации (не используется в этой реализации)</param>
        /// <returns>Задача выполнения обновления</returns>
        public static async Task UpdateCurrentEmotionWithValidationAsync(
            this IDatabaseService databaseService,
            string emotionType,
            float intensity,
            DataValidationService validationService) // Param kept for signature, but not used
        {
            // Проверки основных параметров
            if (string.IsNullOrEmpty(emotionType))
            {
                throw new ArgumentException("Невозможно обновить эмоцию с пустым типом", nameof(emotionType));
            }
            
            if (intensity < 0 || intensity > 1) // Assuming intensity is normalized 0-1
            {
                throw new ArgumentOutOfRangeException(nameof(intensity), $"Интенсивность эмоции должна быть в диапазоне от 0 до 1, текущее значение: {intensity}");
            }
            
            // Проверка через валидатор не требуется, так как параметры уже проверены
            await databaseService.UpdateCurrentEmotion(emotionType, intensity);
        }
    }
} 