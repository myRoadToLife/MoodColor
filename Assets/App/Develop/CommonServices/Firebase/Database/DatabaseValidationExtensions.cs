using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
// using UnityEngine; // Not used
// using App.Develop.Utils.Logging; // MyLogger removed

namespace App.Develop.CommonServices.Firebase.Database
{
    /// <summary>
    /// Расширения для валидации данных в Firebase
    /// </summary>
    public static class DatabaseValidationExtensions
    {
        /// <summary>
        /// Проверяет результат валидации и логирует ошибки в консоль (теперь выбрасывает исключение)
        /// </summary>
        /// <param name="result">Результат валидации</param>
        /// <param name="dataType">Название типа данных (для логирования)</param>
        /// <returns>true если данные валидны (метод теперь void или выбрасывает)</returns>
        public static void CheckAndThrowErrors(this ValidationResult result, string dataType) // Changed return type
        {
            if (result.IsValid)
            {
                return;
            }

            System.Text.StringBuilder errorMessages = new System.Text.StringBuilder();
            errorMessages.AppendLine($"Ошибка валидации данных {dataType}. Количество ошибок: {result.Errors.Count}");
            foreach (ValidationError error in result.Errors) // Explicit type
            {
                errorMessages.AppendLine($"-- Поле: {error.FieldName}, Ошибка: {error.Message}, Код: {error.ErrorCode}");
            }
            throw new ArgumentException(errorMessages.ToString());
        }
        
        /// <summary>
        /// Выполняет валидацию данных с помощью сервиса валидации и возвращает исключение, если данные не валидны
        /// </summary>
        /// <param name="validationService">Сервис валидации</param>
        /// <param name="data">Данные для валидации</param>
        /// <param name="throwException">Выбрасывать исключение при ошибке валидации (параметр теперь не нужен, всегда выбрасывает)</param>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <returns>Результат валидации (если валидно)</returns>
        /// <exception cref="ArgumentException">Исключение, если данные не валидны</exception>
        /// <exception cref="InvalidOperationException">Если валидатор не найден</exception>
        public static ValidationResult ValidateAndThrow<T>(this DataValidationService validationService, T data) // Removed throwException param
        {
            if (!validationService.TryValidate(data, out ValidationResult result)) // Explicit type
            {
                // MyLogger.LogWarning($"Нет зарегистрированного валидатора для типа {typeof(T)}", MyLogger.LogCategory.Firebase); // Warning removed
                throw new InvalidOperationException($"Нет зарегистрированного валидатора для типа {typeof(T)}");
            }

            if (!result.IsValid)
            {
                List<string> errors = new List<string>();
                foreach (ValidationError error in result.Errors) // Explicit type
                {
                    errors.Add($"{error.FieldName}: {error.Message}");
                }
                string errorMessage = string.Join("; ", errors);
                throw new ArgumentException($"Ошибка валидации данных {typeof(T).Name}: {errorMessage}");
            }
            return result;
        }
        
        /// <summary>
        /// Проверяет, является ли запись истории эмоций валидной
        /// </summary>
        /// <param name="record">Запись для проверки</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <returns>true если запись валидна, иначе false</returns>
        public static bool IsValidRecord(this EmotionHistoryRecord record, DataValidationService validationService)
        {
            if (!validationService.TryValidate(record, out ValidationResult result)) // Explicit type
            {
                // MyLogger.LogWarning("Нет зарегистрированного валидатора для EmotionHistoryRecord", MyLogger.LogCategory.Firebase); // Warning removed
                return true; // По умолчанию считаем валидным, если нет валидатора (сохраняем поведение)
            }
            return result.IsValid;
        }
        
        /// <summary>
        /// Проверяет, является ли пользовательский профиль валидным и выбрасывает исключение при ошибке
        /// </summary>
        /// <param name="userData">Данные пользователя для проверки</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <exception cref="ArgumentException">Если данные не валидны</exception>
        /// <exception cref="InvalidOperationException">Если валидатор не найден</exception>
        public static void ValidateUserData(this UserData userData, DataValidationService validationService) // Changed from IsValidUserData to ValidateUserData
        {
            if (!validationService.TryValidate(userData, out ValidationResult result)) // Explicit type
            {
                // MyLogger.LogWarning("Нет зарегистрированного валидатора для UserData", MyLogger.LogCategory.Firebase); // Warning removed
                throw new InvalidOperationException($"Нет зарегистрированного валидатора для типа UserData");
            }
            if (!result.IsValid)
            {
                result.CheckAndThrowErrors("UserData"); // Uses the modified CheckAndThrowErrors
            }
        }
    }
} 