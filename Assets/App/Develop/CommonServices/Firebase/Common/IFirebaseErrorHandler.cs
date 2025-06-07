using System;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Интерфейс для обработки ошибок Firebase с retry логикой
    /// </summary>
    public interface IFirebaseErrorHandler
    {
        /// <summary>
        /// Выполняет операцию с повторными попытками при ошибках
        /// </summary>
        /// <typeparam name="T">Тип результата операции</typeparam>
        /// <param name="operation">Операция для выполнения</param>
        /// <param name="maxRetries">Максимальное количество попыток</param>
        /// <param name="operationDescription">Описание операции для логирования</param>
        /// <returns>Результат операции</returns>
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, string operationDescription = "Unknown operation");

        /// <summary>
        /// Выполняет операцию без возвращаемого значения с повторными попытками
        /// </summary>
        /// <param name="operation">Операция для выполнения</param>
        /// <param name="maxRetries">Максимальное количество попыток</param>
        /// <param name="operationDescription">Описание операции для логирования</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = 3, string operationDescription = "Unknown operation");

        /// <summary>
        /// Проверяет, является ли ошибка повторяемой
        /// </summary>
        /// <param name="exception">Исключение для проверки</param>
        /// <returns>True, если ошибку можно повторить</returns>
        bool IsRetryableError(Exception exception);

        /// <summary>
        /// Вычисляет задержку перед повторной попыткой
        /// </summary>
        /// <param name="attemptNumber">Номер попытки (начинается с 0)</param>
        /// <returns>Время задержки</returns>
        TimeSpan CalculateDelay(int attemptNumber);
    }
}