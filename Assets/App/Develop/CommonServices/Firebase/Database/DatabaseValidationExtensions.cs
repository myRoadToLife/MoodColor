using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database
{
    /// <summary>
    /// Расширения для валидации данных в Firebase
    /// </summary>
    public static class DatabaseValidationExtensions
    {
        /// <summary>
        /// Проверяет результат валидации и логирует ошибки в консоль
        /// </summary>
        /// <param name="result">Результат валидации</param>
        /// <param name="dataType">Название типа данных (для логирования)</param>
        /// <returns>true если данные валидны, иначе false</returns>
        public static bool CheckAndLogErrors(this ValidationResult result, string dataType)
        {
            if (result.IsValid)
            {
                return true;
            }
            
            MyLogger.LogError($"Ошибка валидации данных {dataType}. Количество ошибок: {result.Errors.Count}", MyLogger.LogCategory.Firebase);
            foreach (var error in result.Errors)
            {
                MyLogger.LogError($"-- Поле: {error.FieldName}, Ошибка: {error.Message}, Код: {error.ErrorCode}", MyLogger.LogCategory.Firebase);
            }
            
            return false;
        }
        
        /// <summary>
        /// Выполняет валидацию данных с помощью сервиса валидации и возвращает исключение, если данные не валидны
        /// </summary>
        /// <param name="validationService">Сервис валидации</param>
        /// <param name="data">Данные для валидации</param>
        /// <param name="throwException">Выбрасывать исключение при ошибке валидации</param>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <returns>Результат валидации</returns>
        /// <exception cref="ArgumentException">Исключение, если данные не валидны и throwException=true</exception>
        public static ValidationResult ValidateAndThrow<T>(this DataValidationService validationService, T data, bool throwException = true)
        {
            if (!validationService.TryValidate(data, out var result))
            {
                MyLogger.LogWarning($"Нет зарегистрированного валидатора для типа {typeof(T)}", MyLogger.LogCategory.Firebase);
                return null;
            }
            
            if (!result.IsValid && throwException)
            {
                var errors = new List<string>();
                foreach (var error in result.Errors)
                {
                    errors.Add($"{error.FieldName}: {error.Message}");
                }
                
                var errorMessage = string.Join("; ", errors);
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
            if (!validationService.TryValidate(record, out var result))
            {
                MyLogger.LogWarning("Нет зарегистрированного валидатора для EmotionHistoryRecord", MyLogger.LogCategory.Firebase);
                return true; // По умолчанию считаем валидным, если нет валидатора
            }
            
            return result.IsValid;
        }
        
        /// <summary>
        /// Проверяет, является ли пользовательский профиль валидным
        /// </summary>
        /// <param name="userData">Данные пользователя для проверки</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <returns>true если данные валидны, иначе false</returns>
        public static bool IsValidUserData(this UserData userData, DataValidationService validationService)
        {
            if (!validationService.TryValidate(userData, out var result))
            {
                MyLogger.LogWarning("Нет зарегистрированного валидатора для UserData", MyLogger.LogCategory.Firebase);
                return true; // По умолчанию считаем валидным, если нет валидатора
            }
            
            return result.CheckAndLogErrors("UserData");
        }
    }
} 