using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Common.Helpers
{
    /// <summary>
    /// Помощник для работы с Firebase задачами, унифицирующий обработку исключений и асинхронные операции
    /// </summary>
    public static class FirebaseTaskHelper
    {
        #region Public Methods
        
        /// <summary>
        /// Выполняет Firebase задачу с обработкой исключений
        /// </summary>
        /// <typeparam name="T">Тип результата задачи</typeparam>
        /// <param name="task">Асинхронная Firebase задача</param>
        /// <param name="successMessage">Сообщение для логирования при успехе</param>
        /// <param name="errorMessage">Сообщение для логирования при ошибке</param>
        /// <param name="throwOnError">Выбрасывать ли исключение при ошибке</param>
        /// <returns>Результат выполнения задачи или default(T) при ошибке</returns>
        public static async Task<T> ExecuteWithExceptionHandling<T>(
            Task<T> task, 
            string successMessage = null, 
            string errorMessage = null, 
            bool throwOnError = false)
        {
            try
            {
                var result = await task;
                
                if (!string.IsNullOrEmpty(successMessage))
                {
                    Debug.Log($"✅ {successMessage}");
                }
                
                return result;
            }
            catch (FirebaseException ex)
            {
                HandleFirebaseException(ex, errorMessage);
                
                if (throwOnError)
                {
                    throw;
                }
                
                return default;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ {errorMessage ?? "Ошибка Firebase"}: {ex.Message}");
                
                if (throwOnError)
                {
                    throw;
                }
                
                return default;
            }
        }

        /// <summary>
        /// Выполняет Firebase задачу без результата с обработкой исключений
        /// </summary>
        /// <param name="task">Асинхронная Firebase задача</param>
        /// <param name="successMessage">Сообщение для логирования при успехе</param>
        /// <param name="errorMessage">Сообщение для логирования при ошибке</param>
        /// <param name="throwOnError">Выбрасывать ли исключение при ошибке</param>
        /// <returns>True при успешном выполнении, иначе False</returns>
        public static async Task<bool> ExecuteWithExceptionHandling(
            Task task, 
            string successMessage = null, 
            string errorMessage = null, 
            bool throwOnError = false)
        {
            try
            {
                await task;
                
                if (!string.IsNullOrEmpty(successMessage))
                {
                    Debug.Log($"✅ {successMessage}");
                }
                
                return true;
            }
            catch (FirebaseException ex)
            {
                HandleFirebaseException(ex, errorMessage);
                
                if (throwOnError)
                {
                    throw;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ {errorMessage ?? "Ошибка Firebase"}: {ex.Message}");
                
                if (throwOnError)
                {
                    throw;
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// Возвращает пользовательское сообщение об ошибке на основе исключения Firebase
        /// </summary>
        /// <param name="ex">Исключение Firebase</param>
        /// <returns>Пользовательское сообщение об ошибке</returns>
        public static string GetFriendlyErrorMessage(Exception ex)
        {
            string message = ex.Message.ToLower();

            // Ошибки аутентификации
            if (message.Contains("invalid_email")) return "Некорректный формат email";
            if (message.Contains("wrong_password")) return "Неверный пароль";
            if (message.Contains("user_not_found")) return "Пользователь не найден";
            if (message.Contains("user_disabled")) return "Аккаунт заблокирован";
            if (message.Contains("too_many_requests")) return "Слишком много попыток. Попробуйте позже";
            if (message.Contains("operation_not_allowed")) return "Операция не разрешена";
            if (message.Contains("requires_recent_login")) return "Требуется повторный вход";
            if (message.Contains("weak_password")) return "Слишком простой пароль";
            if (message.Contains("email_already_in_use")) return "Email уже используется";
            
            // Ошибки базы данных
            if (message.Contains("permission_denied")) return "Доступ запрещен";
            if (message.Contains("unavailable")) return "Сервис недоступен. Проверьте подключение к интернету";
            if (message.Contains("network_error")) return "Ошибка сети. Проверьте подключение к интернету";
            
            // Общие ошибки
            if (message.Contains("cancelled")) return "Операция отменена";
            if (message.Contains("timeout")) return "Превышено время ожидания";

            return "Произошла ошибка. Попробуйте позже";
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Обрабатывает исключение Firebase
        /// </summary>
        /// <param name="ex">Исключение Firebase</param>
        /// <param name="customErrorMessage">Пользовательское сообщение об ошибке</param>
        private static void HandleFirebaseException(FirebaseException ex, string customErrorMessage = null)
        {
            string errorCode = string.Empty;
            string errorType = "Firebase";
            
            // Упрощаем обработку исключений, избегая сложных проверок типов
            if (ex.GetType().Name.Contains("Auth"))
            {
                errorType = "Auth";
                errorCode = "[Auth] ";
            }
            else if (ex.GetType().Name.Contains("Database"))
            {
                errorType = "Database";
                errorCode = "[DB] ";
            }
            
            string friendlyMessage = GetFriendlyErrorMessage(ex);
            string logMessage = $"❌ {errorType} ошибка {errorCode}: {ex.Message}";
            
            if (!string.IsNullOrEmpty(customErrorMessage))
            {
                logMessage = $"❌ {customErrorMessage}: {friendlyMessage} ({errorCode}{ex.Message})";
            }
            
            Debug.LogError(logMessage);
        }
        
        #endregion
    }
} 