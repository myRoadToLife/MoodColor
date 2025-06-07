using System;
using System.Threading.Tasks;
using Firebase;
using App.Develop.Utils.Logging;
using Firebase.Database;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Обработчик ошибок Firebase с retry логикой и exponential backoff
    /// </summary>
    public class FirebaseErrorHandler : IFirebaseErrorHandler
    {
        #region Constants

        private const int DefaultMaxRetries = 3;
        private const int BaseDelayMs = 1000; // 1 секунда базовая задержка
        private const int MaxDelayMs = 30000; // 30 секунд максимальная задержка

        #endregion

        #region Public Methods

        /// <summary>
        /// Выполняет операцию с повторными попытками при ошибках
        /// </summary>
        /// <typeparam name="T">Тип результата операции</typeparam>
        /// <param name="operation">Операция для выполнения</param>
        /// <param name="maxRetries">Максимальное количество попыток</param>
        /// <param name="operationDescription">Описание операции для логирования</param>
        /// <returns>Результат операции</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = DefaultMaxRetries, string operationDescription = "Unknown operation")
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            Exception lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        MyLogger.Log($"🔄 [FirebaseErrorHandler] Попытка {attempt + 1}/{maxRetries + 1} для операции: {operationDescription}", MyLogger.LogCategory.Firebase);
                    }

                    var result = await operation();

                    if (attempt > 0)
                    {
                        MyLogger.Log($"✅ [FirebaseErrorHandler] Операция успешна на попытке {attempt + 1}: {operationDescription}", MyLogger.LogCategory.Firebase);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (!IsRetryableError(ex))
                    {
                        MyLogger.LogError($"❌ [FirebaseErrorHandler] Неповторяемая ошибка: {operationDescription}, ошибка: {ex.Message}", MyLogger.LogCategory.Firebase);
                        throw;
                    }

                    if (attempt == maxRetries)
                    {
                        MyLogger.LogError($"❌ [FirebaseErrorHandler] Все попытки исчерпаны для операции: {operationDescription}, последняя ошибка: {ex.Message}", MyLogger.LogCategory.Firebase);
                        break;
                    }

                    var delay = CalculateDelay(attempt);
                    MyLogger.Log($"⏳ [FirebaseErrorHandler] Ошибка на попытке {attempt + 1}, ждем {delay.TotalSeconds:F1}с перед повтором: {ex.Message}", MyLogger.LogCategory.Firebase);

                    await Task.Delay(delay);
                }
            }

            throw lastException ?? new InvalidOperationException($"Операция {operationDescription} не выполнена после {maxRetries + 1} попыток");
        }

        /// <summary>
        /// Выполняет операцию без возвращаемого значения с повторными попытками
        /// </summary>
        /// <param name="operation">Операция для выполнения</param>
        /// <param name="maxRetries">Максимальное количество попыток</param>
        /// <param name="operationDescription">Описание операции для логирования</param>
        /// <returns>True, если операция выполнена успешно</returns>
        public async Task<bool> ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = DefaultMaxRetries, string operationDescription = "Unknown operation")
        {
            try
            {
                await ExecuteWithRetryAsync(async () =>
                {
                    await operation();
                    return true; // Возвращаем успех
                }, maxRetries, operationDescription);

                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [FirebaseErrorHandler] Операция окончательно неудачна: {operationDescription}, ошибка: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Проверяет, является ли ошибка повторяемой
        /// </summary>
        /// <param name="exception">Исключение для проверки</param>
        /// <returns>True, если ошибку можно повторить</returns>
        public bool IsRetryableError(Exception exception)
        {
            if (exception == null)
                return false;

            // Firebase Database специфичные ошибки
            if (exception is FirebaseException firebaseEx)
            {
                MyLogger.Log($"🔍 [FirebaseErrorHandler] Анализируем Firebase ошибку: ErrorCode={firebaseEx.ErrorCode}, Message={firebaseEx.Message}", MyLogger.LogCategory.Firebase);

                // Временные ошибки сети или сервера можно повторить
                switch (firebaseEx.ErrorCode)
                {
                    case (int)DatabaseError.NetworkError:
                    case (int)DatabaseError.Unavailable:
                    case (int)DatabaseError.Disconnected:
                        MyLogger.Log($"🔄 [FirebaseErrorHandler] Повторяемая Firebase ошибка: {firebaseEx.ErrorCode}", MyLogger.LogCategory.Firebase);
                        return true;

                    // Ошибки авторизации и валидации данных повторять не нужно
                    case (int)DatabaseError.PermissionDenied:
                    case (int)DatabaseError.InvalidToken:
                    case (int)DatabaseError.ExpiredToken:
                        MyLogger.Log($"❌ [FirebaseErrorHandler] Неповторяемая Firebase ошибка: {firebaseEx.ErrorCode}", MyLogger.LogCategory.Firebase);
                        return false;

                    default:
                        // Для неизвестных ошибок Firebase лучше не повторять
                        MyLogger.Log($"⚠️ [FirebaseErrorHandler] Неизвестная Firebase ошибка, не повторяем: {firebaseEx.ErrorCode}", MyLogger.LogCategory.Firebase);
                        return false;
                }
            }

            // Общие сетевые ошибки
            if (exception is System.Net.WebException ||
                exception is System.Net.Http.HttpRequestException ||
                exception is TaskCanceledException ||
                exception is TimeoutException)
            {
                MyLogger.Log($"🔄 [FirebaseErrorHandler] Повторяемая сетевая ошибка: {exception.GetType().Name}", MyLogger.LogCategory.Firebase);
                return true;
            }

            // OperationCanceledException может быть связана с таймаутами
            if (exception is OperationCanceledException)
            {
                MyLogger.Log($"🔄 [FirebaseErrorHandler] Повторяемая ошибка отмены операции: {exception.Message}", MyLogger.LogCategory.Firebase);
                return true;
            }

            // Проверяем сообщение ошибки на наличие ключевых слов
            var errorMessage = exception.Message?.ToLower() ?? "";
            if (errorMessage.Contains("network") ||
                errorMessage.Contains("timeout") ||
                errorMessage.Contains("connection") ||
                errorMessage.Contains("unavailable"))
            {
                MyLogger.Log($"🔄 [FirebaseErrorHandler] Повторяемая ошибка по ключевым словам: {exception.Message}", MyLogger.LogCategory.Firebase);
                return true;
            }

            // По умолчанию не повторяем
            MyLogger.Log($"❌ [FirebaseErrorHandler] Неповторяемая ошибка: {exception.GetType().Name} - {exception.Message}", MyLogger.LogCategory.Firebase);
            return false;
        }

        /// <summary>
        /// Вычисляет задержку перед повторной попыткой с использованием exponential backoff
        /// </summary>
        /// <param name="attemptNumber">Номер попытки (начинается с 0)</param>
        /// <returns>Время задержки</returns>
        public TimeSpan CalculateDelay(int attemptNumber)
        {
            if (attemptNumber < 0)
                return TimeSpan.Zero;

            // Exponential backoff: 1s, 2s, 4s, 8s, 16s...
            var delayMs = BaseDelayMs * Math.Pow(2, attemptNumber);

            // Ограничиваем максимальную задержку
            delayMs = Math.Min(delayMs, MaxDelayMs);

            // Добавляем небольшой случайный jitter (±20%) для предотвращения thundering herd
            var random = new Random();
            var jitterFactor = 0.8 + (random.NextDouble() * 0.4); // 0.8 - 1.2
            delayMs *= jitterFactor;

            return TimeSpan.FromMilliseconds(delayMs);
        }

        #endregion
    }
}