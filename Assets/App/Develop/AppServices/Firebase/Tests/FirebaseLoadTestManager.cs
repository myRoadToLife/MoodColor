#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Инструмент для проведения нагрузочных тестов Firebase
    /// </summary>
    public class FirebaseLoadTestManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Test Configuration")]
        [SerializeField] private int m_ConcurrentOperationsCount = 50;
        [SerializeField] private int m_TotalOperationsCount = 1000;
        [SerializeField] private float m_DelayBetweenBatches = 0.1f;
        
        [Header("Test Types")]
        [SerializeField] private bool m_TestReads = true;
        [SerializeField] private bool m_TestWrites = true;
        [SerializeField] private bool m_TestDeletes = false;
        [SerializeField] private bool m_TestRealTimeUpdates = true;
        
        [Header("Test Results")]
        [SerializeField] private string m_TestStatus = "Ready";
        [SerializeField] private float m_AverageResponseTime;
        [SerializeField] private float m_MaxResponseTime;
        [SerializeField] private int m_SuccessfulOperations;
        [SerializeField] private int m_FailedOperations;
        [SerializeField] private float m_TestProgress;
        #endregion

        #region Private Fields
        private IFirebaseDatabaseService m_DatabaseService;
        private IFirebaseConnectionManager m_ConnectionManager;
        private IFirebaseCacheManager m_CacheManager;
        
        private readonly List<float> m_ResponseTimes = new List<float>();
        private readonly List<string> m_TestErrors = new List<string>();
        
        private bool m_IsTestRunning;
        private int m_CompletedOperations;
        private Stopwatch m_TestStopwatch = new Stopwatch();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Получаем реальные сервисы из DI-контейнера или ServiceLocator
            m_DatabaseService = ServiceLocator.Get<IFirebaseDatabaseService>();
            m_ConnectionManager = ServiceLocator.Get<IFirebaseConnectionManager>();
            m_CacheManager = ServiceLocator.Get<IFirebaseCacheManager>();
            
            // Если сервисы не найдены, выводим предупреждение
            if (m_DatabaseService == null || m_ConnectionManager == null || m_CacheManager == null)
            {
                Debug.LogError("Firebase services not available. Please ensure they are registered in the ServiceLocator.");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Запускает процесс нагрузочного тестирования
        /// </summary>
        public void StartLoadTest()
        {
            if (m_IsTestRunning)
            {
                Debug.LogWarning("Test is already running!");
                return;
            }
            
            StartCoroutine(RunLoadTest());
        }

        /// <summary>
        /// Экспортирует результаты тестирования в файл
        /// </summary>
        public void ExportResults()
        {
            string results = GenerateTestReport();
            string path = $"{Application.persistentDataPath}/firebase_load_test_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            try
            {
                System.IO.File.WriteAllText(path, results);
                Debug.Log($"Results exported to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export results: {e.Message}");
            }
        }

        /// <summary>
        /// Прерывает текущий тест
        /// </summary>
        public void AbortTest()
        {
            if (!m_IsTestRunning) return;
            
            m_IsTestRunning = false;
            m_TestStatus = "Aborted";
            
            Debug.Log("Load test aborted by user");
        }
        #endregion

        #region Private Methods
        private IEnumerator RunLoadTest()
        {
            // Проверяем, доступны ли сервисы
            if (m_DatabaseService == null)
            {
                Debug.LogError("Firebase services not available. Test aborted.");
                yield break;
            }
            
            // Сброс результатов предыдущего теста
            ResetTestState();
            
            m_IsTestRunning = true;
            m_TestStopwatch.Start();
            m_TestStatus = "Running";
            
            Debug.Log($"Starting Firebase load test with {m_TotalOperationsCount} operations, {m_ConcurrentOperationsCount} concurrent");

            // Основной цикл теста
            for (int i = 0; i < m_TotalOperationsCount; i += m_ConcurrentOperationsCount)
            {
                if (!m_IsTestRunning) break;
                
                int batchSize = Math.Min(m_ConcurrentOperationsCount, m_TotalOperationsCount - i);
                
                // Запуск пакета операций
                for (int j = 0; j < batchSize; j++)
                {
                    int operationType = UnityEngine.Random.Range(0, 4);
                    
                    switch (operationType)
                    {
                        case 0 when m_TestReads:
                            PerformReadOperation();
                            break;
                        case 1 when m_TestWrites:
                            PerformWriteOperation();
                            break;
                        case 2 when m_TestDeletes:
                            PerformDeleteOperation();
                            break;
                        case 3 when m_TestRealTimeUpdates:
                            PerformRealTimeUpdateTest();
                            break;
                        default:
                            // Выполняем чтение как операцию по умолчанию
                            PerformReadOperation();
                            break;
                    }
                }
                
                // Обновляем прогресс
                m_TestProgress = (float)m_CompletedOperations / m_TotalOperationsCount;
                
                // Добавляем задержку между пакетами для избежания блокировки Firebase
                yield return new WaitForSeconds(m_DelayBetweenBatches);
            }
            
            // Ждем завершения всех операций
            while (m_CompletedOperations < m_TotalOperationsCount && m_IsTestRunning)
            {
                m_TestProgress = (float)m_CompletedOperations / m_TotalOperationsCount;
                yield return new WaitForSeconds(0.1f);
            }
            
            FinishTest();
        }

        private void PerformReadOperation()
        {
            string testPath = $"test/load_testing/read_{Guid.NewGuid()}";
            Stopwatch operationTimer = Stopwatch.StartNew();
            
            m_DatabaseService.GetDataAsync<object>(testPath)
                .ContinueWith(task => 
                {
                    operationTimer.Stop();
                    float responseTime = operationTimer.ElapsedMilliseconds / 1000f;
                    
                    if (task.IsFaulted)
                    {
                        RegisterFailedOperation(responseTime, "Read", task.Exception);
                    }
                    else
                    {
                        RegisterSuccessfulOperation(responseTime);
                    }
                });
        }

        private void PerformWriteOperation()
        {
            string testPath = $"test/load_testing/write_{Guid.NewGuid()}";
            Stopwatch operationTimer = Stopwatch.StartNew();
            
            // Создаем тестовые данные
            var testData = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "value", UnityEngine.Random.Range(0, 1000) },
                { "text", $"Test data {Guid.NewGuid()}" }
            };
            
            m_DatabaseService.SetDataAsync(testPath, testData)
                .ContinueWith(task => 
                {
                    operationTimer.Stop();
                    float responseTime = operationTimer.ElapsedMilliseconds / 1000f;
                    
                    if (task.IsFaulted)
                    {
                        RegisterFailedOperation(responseTime, "Write", task.Exception);
                    }
                    else
                    {
                        RegisterSuccessfulOperation(responseTime);
                    }
                });
        }

        private void PerformDeleteOperation()
        {
            string testPath = $"test/load_testing/delete_{Guid.NewGuid()}";
            Stopwatch operationTimer = Stopwatch.StartNew();
            
            // Сначала создаем данные, которые будем удалять
            var testData = new Dictionary<string, object>
            {
                { "to_delete", true },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };
            
            m_DatabaseService.SetDataAsync(testPath, testData)
                .ContinueWith(setTask => 
                {
                    if (setTask.IsFaulted)
                    {
                        operationTimer.Stop();
                        RegisterFailedOperation(operationTimer.ElapsedMilliseconds / 1000f, "Delete-SetUp", setTask.Exception);
                        return;
                    }
                    
                    // Затем удаляем данные
                    m_DatabaseService.DeleteDataAsync(testPath)
                        .ContinueWith(deleteTask => 
                        {
                            operationTimer.Stop();
                            float responseTime = operationTimer.ElapsedMilliseconds / 1000f;
                            
                            if (deleteTask.IsFaulted)
                            {
                                RegisterFailedOperation(responseTime, "Delete", deleteTask.Exception);
                            }
                            else
                            {
                                RegisterSuccessfulOperation(responseTime);
                            }
                        });
                });
        }

        private void PerformRealTimeUpdateTest()
        {
            string testPath = $"test/load_testing/realtime_{Guid.NewGuid()}";
            Stopwatch operationTimer = Stopwatch.StartNew();
            
            // Флаг для отслеживания получения обновления
            bool updateReceived = false;
            
            // Подписываемся на обновления
            m_DatabaseService.AddRealtimeListener<Dictionary<string, object>>(testPath, data => 
            {
                if (data != null && !updateReceived)
                {
                    updateReceived = true;
                    operationTimer.Stop();
                    float responseTime = operationTimer.ElapsedMilliseconds / 1000f;
                    
                    RegisterSuccessfulOperation(responseTime);
                    
                    // Отписываемся от обновлений после первого получения
                    m_DatabaseService.RemoveRealtimeListener(testPath);
                }
            });
            
            // Публикуем данные для тестирования обновления
            var testData = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "value", UnityEngine.Random.Range(0, 1000) }
            };
            
            m_DatabaseService.SetDataAsync(testPath, testData)
                .ContinueWith(task => 
                {
                    if (task.IsFaulted)
                    {
                        operationTimer.Stop();
                        RegisterFailedOperation(operationTimer.ElapsedMilliseconds / 1000f, "RealTime", task.Exception);
                        m_DatabaseService.RemoveRealtimeListener(testPath);
                    }
                });
            
            // Добавляем автоматическое завершение по таймауту
            StartCoroutine(RealtimeTimeout(testPath, operationTimer));
        }

        private IEnumerator RealtimeTimeout(string path, Stopwatch timer)
        {
            // Таймаут для получения обновления (5 секунд)
            yield return new WaitForSeconds(5.0f);
            
            // Если операция все еще выполняется
            if (timer.IsRunning)
            {
                timer.Stop();
                
                // Регистрируем ошибку таймаута
                RegisterFailedOperation(
                    timer.ElapsedMilliseconds / 1000f, 
                    "RealTime", 
                    new TimeoutException("Realtime update timeout")
                );
                
                // Отписываемся от обновлений
                m_DatabaseService.RemoveRealtimeListener(path);
            }
        }

        private void RegisterSuccessfulOperation(float responseTime)
        {
            lock (m_ResponseTimes)
            {
                m_SuccessfulOperations++;
                m_CompletedOperations++;
                m_ResponseTimes.Add(responseTime);
                
                if (responseTime > m_MaxResponseTime)
                {
                    m_MaxResponseTime = responseTime;
                }
                
                UpdateAverageResponseTime();
            }
        }

        private void RegisterFailedOperation(float responseTime, string operationType, Exception exception)
        {
            lock (m_ResponseTimes)
            {
                m_FailedOperations++;
                m_CompletedOperations++;
                m_ResponseTimes.Add(responseTime);
                
                string errorMsg = $"{operationType} operation failed after {responseTime:F2}s: {exception?.Message}";
                m_TestErrors.Add(errorMsg);
                Debug.LogError(errorMsg);
                
                UpdateAverageResponseTime();
            }
        }

        private void UpdateAverageResponseTime()
        {
            if (m_ResponseTimes.Count == 0) return;
            
            float sum = 0;
            foreach (float time in m_ResponseTimes)
            {
                sum += time;
            }
            
            m_AverageResponseTime = sum / m_ResponseTimes.Count;
        }

        private void ResetTestState()
        {
            m_ResponseTimes.Clear();
            m_TestErrors.Clear();
            m_SuccessfulOperations = 0;
            m_FailedOperations = 0;
            m_CompletedOperations = 0;
            m_AverageResponseTime = 0;
            m_MaxResponseTime = 0;
            m_TestProgress = 0;
            m_TestStopwatch.Reset();
        }

        private void FinishTest()
        {
            m_IsTestRunning = false;
            m_TestStopwatch.Stop();
            float totalTime = m_TestStopwatch.ElapsedMilliseconds / 1000f;
            
            m_TestStatus = "Completed";
            
            Debug.Log($"Load test completed in {totalTime:F2}s");
            Debug.Log($"Successful operations: {m_SuccessfulOperations}/{m_TotalOperationsCount} ({(float)m_SuccessfulOperations/m_TotalOperationsCount*100:F1}%)");
            Debug.Log($"Average response time: {m_AverageResponseTime:F3}s, Max: {m_MaxResponseTime:F3}s");
            
            if (m_FailedOperations > 0)
            {
                Debug.LogWarning($"Failed operations: {m_FailedOperations}/{m_TotalOperationsCount} ({(float)m_FailedOperations/m_TotalOperationsCount*100:F1}%)");
            }
        }

        private string GenerateTestReport()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            
            report.AppendLine("========= FIREBASE LOAD TEST REPORT =========");
            report.AppendLine($"Date: {DateTime.Now}");
            report.AppendLine($"Status: {m_TestStatus}");
            report.AppendLine();
            
            report.AppendLine("TEST CONFIGURATION:");
            report.AppendLine($"Total operations: {m_TotalOperationsCount}");
            report.AppendLine($"Concurrent operations: {m_ConcurrentOperationsCount}");
            report.AppendLine($"Tests enabled: Reads={m_TestReads}, Writes={m_TestWrites}, Deletes={m_TestDeletes}, RealTime={m_TestRealTimeUpdates}");
            report.AppendLine();
            
            report.AppendLine("TEST RESULTS:");
            report.AppendLine($"Successful operations: {m_SuccessfulOperations}/{m_TotalOperationsCount} ({(float)m_SuccessfulOperations/m_TotalOperationsCount*100:F1}%)");
            report.AppendLine($"Failed operations: {m_FailedOperations}/{m_TotalOperationsCount} ({(float)m_FailedOperations/m_TotalOperationsCount*100:F1}%)");
            report.AppendLine($"Average response time: {m_AverageResponseTime:F3}s");
            report.AppendLine($"Maximum response time: {m_MaxResponseTime:F3}s");
            report.AppendLine($"Total test duration: {m_TestStopwatch.ElapsedMilliseconds/1000f:F2}s");
            report.AppendLine();
            
            if (m_TestErrors.Count > 0)
            {
                report.AppendLine("ERRORS:");
                foreach (string error in m_TestErrors)
                {
                    report.AppendLine($"- {error}");
                }
                report.AppendLine();
            }
            
            report.AppendLine("RECOMMENDATIONS:");
            
            // Анализ результатов и рекомендации
            if (m_AverageResponseTime > 2.0f)
            {
                report.AppendLine("- Среднее время отклика превышает целевое (2 секунды). Рекомендуется оптимизировать структуру данных.");
            }
            
            if (m_FailedOperations > m_TotalOperationsCount * 0.05f)
            {
                report.AppendLine("- Высокий процент неудачных операций. Рекомендуется проверить обработку ошибок и улучшить механизм повторных попыток.");
            }
            
            if (m_MaxResponseTime > 5.0f)
            {
                report.AppendLine("- Обнаружены аномально долгие операции. Рекомендуется исследовать причины задержек.");
            }
            
            return report.ToString();
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Run Test")]
        private void RunTestFromEditor()
        {
            StartLoadTest();
        }
        
        [ContextMenu("Generate Test Report")]
        private void GenerateReportFromEditor()
        {
            Debug.Log(GenerateTestReport());
        }
        #endif
    }
}
#endif 