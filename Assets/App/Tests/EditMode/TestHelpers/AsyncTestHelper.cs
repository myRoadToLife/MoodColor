using System;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Tests.EditMode.TestHelpers
{
    /// <summary>
    /// Вспомогательный класс для асинхронного тестирования в Unity
    /// </summary>
    public static class AsyncTestHelper
    {
        /// <summary>
        /// Выполняет асинхронную операцию и ожидает её завершения
        /// </summary>
        /// <param name="task">Задача для выполнения</param>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        public static void RunSync(Task task, int timeout = 5000)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var startTime = DateTime.Now;
            var asyncOperation = task;
            
            while (!asyncOperation.IsCompleted)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    Debug.LogError($"Операция не завершена в течение {timeout} мс");
                    throw new TimeoutException($"Операция не завершена в течение {timeout} мс");
                }
                
                // Небольшая задержка для освобождения потока
                System.Threading.Thread.Sleep(10);
            }
            
            if (asyncOperation.IsFaulted && asyncOperation.Exception != null)
            {
                Debug.LogError($"Исключение при выполнении задачи: {asyncOperation.Exception}");
                throw asyncOperation.Exception;
            }
        }
        
        /// <summary>
        /// Выполняет асинхронную операцию и возвращает результат
        /// </summary>
        /// <typeparam name="T">Тип результата</typeparam>
        /// <param name="task">Задача для выполнения</param>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        /// <returns>Результат асинхронной операции</returns>
        public static T RunSync<T>(Task<T> task, int timeout = 5000)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var startTime = DateTime.Now;
            var asyncOperation = task;
            
            while (!asyncOperation.IsCompleted)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    Debug.LogError($"Операция не завершена в течение {timeout} мс");
                    throw new TimeoutException($"Операция не завершена в течение {timeout} мс");
                }
                
                // Небольшая задержка для освобождения потока
                System.Threading.Thread.Sleep(10);
            }
            
            if (asyncOperation.IsFaulted && asyncOperation.Exception != null)
            {
                Debug.LogError($"Исключение при выполнении задачи: {asyncOperation.Exception}");
                throw asyncOperation.Exception;
            }
            
            return asyncOperation.Result;
        }
        
        /// <summary>
        /// Ожидает выполнения предиката в течение указанного времени
        /// </summary>
        /// <param name="predicate">Предикат для проверки</param>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        /// <param name="checkInterval">Интервал проверки в миллисекундах</param>
        /// <returns>true, если предикат выполнен в течение таймаута</returns>
        public static bool WaitUntil(Func<bool> predicate, int timeout = 5000, int checkInterval = 100)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            
            var startTime = DateTime.Now;
            
            while (!predicate())
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    Debug.LogWarning($"Предикат не был выполнен в течение {timeout} мс");
                    return false;
                }
                
                System.Threading.Thread.Sleep(checkInterval);
            }
            
            return true;
        }
    }
} 