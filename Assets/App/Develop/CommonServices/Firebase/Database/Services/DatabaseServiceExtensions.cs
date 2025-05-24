using App.Develop.CommonServices.Firebase.Database.Models;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.Utils.Logging;

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
        public static async Task<bool> SaveEmotionHistoryRecordWithValidation(
            this IDatabaseService databaseService, 
            EmotionHistoryRecord record,
            DataValidationService validationService)
        {
            if (record == null)
            {
                MyLogger.LogError("Невозможно сохранить пустую запись", MyLogger.LogCategory.Firebase);
                return false;
            }
            
            // Если сервис валидации не доступен, пропускаем валидацию
            if (validationService == null || !validationService.HasValidator<EmotionHistoryRecord>())
            {
                MyLogger.LogWarning("Валидация пропущена: сервис валидации не доступен", MyLogger.LogCategory.Firebase);
                return await databaseService.SaveEmotionHistoryRecord(record);
            }
            
            // Выполняем валидацию
            var validationResult = validationService.Validate<EmotionHistoryRecord>(record);
            if (!validationResult.IsValid)
            {
                validationResult.CheckAndLogErrors("EmotionHistoryRecord");
                return false;
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
        public static async Task<bool> UpdateUserDataWithValidation(
            this IDatabaseService databaseService,
            UserData userData,
            DataValidationService validationService)
        {
            if (userData == null)
            {
                MyLogger.LogError("Невозможно обновить пустые данные пользователя", MyLogger.LogCategory.Firebase);
                return false;
            }
            
            // Если сервис валидации не доступен, пропускаем валидацию
            if (validationService == null || !validationService.HasValidator<UserData>())
            {
                MyLogger.LogWarning("Валидация пропущена: сервис валидации не доступен", MyLogger.LogCategory.Firebase);
                await databaseService.UpdateUserProfile(userData.Profile);
                return true; // Предполагаем успех, если нет явных ошибок
            }
            
            // Выполняем валидацию
            var validationResult = validationService.Validate<UserData>(userData);
            if (!validationResult.IsValid)
            {
                validationResult.CheckAndLogErrors("UserData");
                return false;
            }
            
            // Обновляем валидные данные
            await databaseService.UpdateUserProfile(userData.Profile);
            return true; // Предполагаем успех, если нет явных ошибок
        }
        
        /// <summary>
        /// Обновляет текущую эмоцию пользователя с предварительной валидацией
        /// </summary>
        /// <param name="databaseService">Сервис базы данных</param>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="intensity">Интенсивность эмоции</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <returns>Задача выполнения обновления</returns>
        public static async Task UpdateCurrentEmotionWithValidation(
            this IDatabaseService databaseService,
            string emotionType,
            float intensity,
            DataValidationService validationService)
        {
            // Проверки основных параметров
            if (string.IsNullOrEmpty(emotionType))
            {
                MyLogger.LogError("Невозможно обновить эмоцию с пустым типом", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (intensity < 0 || intensity > 1)
            {
                MyLogger.LogError($"Интенсивность эмоции должна быть в диапазоне от 0 до 1, текущее значение: {intensity}", MyLogger.LogCategory.Firebase);
                return;
            }
            
            // Проверка через валидатор не требуется, так как параметры уже проверены
            await databaseService.UpdateCurrentEmotion(emotionType, intensity);
        }
    }
} 