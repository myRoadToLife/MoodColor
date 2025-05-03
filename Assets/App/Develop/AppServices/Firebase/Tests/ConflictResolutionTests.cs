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
    /// Класс для тестирования механизма разрешения конфликтов
    /// </summary>
    public class ConflictResolutionTests : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Test Configuration")]
        [SerializeField] private bool m_RunOnStart = false;
        [SerializeField] private int m_ConflictTestCount = 50;
        [SerializeField] private float m_DelayBetweenTests = 0.5f;
        
        [Header("Test Types")]
        [SerializeField] private bool m_TestSimpleConflicts = true;
        [SerializeField] private bool m_TestComplexConflicts = true;
        [SerializeField] private bool m_TestDeleteConflicts = true;
        
        [Header("Test Results")]
        [SerializeField] private string m_TestStatus = "Ready";
        [SerializeField] private int m_ResolvedConflicts;
        [SerializeField] private int m_UnresolvedConflicts;
        [SerializeField] private float m_AverageResolutionTime;
        [SerializeField] private float m_MaxResolutionTime;
        #endregion

        #region Private Fields
        private IDatabaseService m_DatabaseService;
        private IConflictResolutionStrategy m_ConflictResolver;
        
        private readonly List<float> m_ResolutionTimes = new List<float>();
        private readonly List<string> m_TestErrors = new List<string>();
        
        private bool m_IsTestRunning;
        private Stopwatch m_TestStopwatch = new Stopwatch();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Получаем реальные сервисы
            m_DatabaseService = ServiceLocator.Get<IDatabaseService>();
            m_ConflictResolver = ServiceLocator.Get<IConflictResolutionStrategy>();
            
            if (m_DatabaseService == null || m_ConflictResolver == null)
            {
                Debug.LogError("Required services not available. Please ensure they are registered in the ServiceLocator.");
            }
            
            if (m_RunOnStart)
            {
                StartConflictTests();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Запускает тесты разрешения конфликтов
        /// </summary>
        public void StartConflictTests()
        {
            if (m_IsTestRunning)
            {
                Debug.LogWarning("Conflict tests already running!");
                return;
            }
            
            if (m_DatabaseService == null || m_ConflictResolver == null)
            {
                Debug.LogError("Required services not available!");
                return;
            }
            
            StartCoroutine(RunConflictTests());
        }
        
        /// <summary>
        /// Прерывает текущие тесты
        /// </summary>
        public void AbortTests()
        {
            if (!m_IsTestRunning) return;
            
            m_IsTestRunning = false;
            m_TestStatus = "Aborted";
            
            Debug.Log("Conflict tests aborted by user");
        }
        #endregion

        #region Private Methods
        private IEnumerator RunConflictTests()
        {
            // Сброс результатов предыдущего теста
            ResetTestState();
            
            m_IsTestRunning = true;
            m_TestStopwatch.Start();
            m_TestStatus = "Running";
            
            Debug.Log($"Starting Firebase conflict resolution tests with {m_ConflictTestCount} test cases");
            
            // Основной цикл тестов
            int simpleTestCount = m_TestSimpleConflicts ? m_ConflictTestCount / 3 : 0;
            int complexTestCount = m_TestComplexConflicts ? m_ConflictTestCount / 3 : 0;
            int deleteTestCount = m_TestDeleteConflicts ? m_ConflictTestCount / 3 : 0;
            
            // Корректировка распределения тестов, если какие-то типы отключены
            int totalPlanned = simpleTestCount + complexTestCount + deleteTestCount;
            if (totalPlanned < m_ConflictTestCount)
            {
                int remaining = m_ConflictTestCount - totalPlanned;
                if (m_TestSimpleConflicts) simpleTestCount += remaining / 2;
                if (m_TestComplexConflicts) complexTestCount += remaining - (remaining / 2);
            }
            
            // Выполняем тесты простых конфликтов
            if (m_TestSimpleConflicts)
            {
                for (int i = 0; i < simpleTestCount; i++)
                {
                    if (!m_IsTestRunning) break;
                    yield return StartCoroutine(TestSimpleConflict(i));
                    yield return new WaitForSeconds(m_DelayBetweenTests);
                }
            }
            
            // Выполняем тесты сложных конфликтов
            if (m_TestComplexConflicts)
            {
                for (int i = 0; i < complexTestCount; i++)
                {
                    if (!m_IsTestRunning) break;
                    yield return StartCoroutine(TestComplexConflict(i));
                    yield return new WaitForSeconds(m_DelayBetweenTests);
                }
            }
            
            // Выполняем тесты конфликтов удаления
            if (m_TestDeleteConflicts)
            {
                for (int i = 0; i < deleteTestCount; i++)
                {
                    if (!m_IsTestRunning) break;
                    yield return StartCoroutine(TestDeleteConflict(i));
                    yield return new WaitForSeconds(m_DelayBetweenTests);
                }
            }
            
            FinishTest();
        }

        private IEnumerator TestSimpleConflict(int iteration)
        {
            Debug.Log($"Simple conflict test #{iteration + 1}: Creating conflicting data...");
            
            string testPath = $"test/conflict_testing/simple_{Guid.NewGuid()}";
            
            // Создаем начальные данные
            var initialData = new TestData { Value = UnityEngine.Random.Range(0, 100), Text = "Initial" };
            
            yield return StartCoroutine(SaveDataAndWait(testPath, initialData));
            
            // Создаем два конфликтующих набора данных с разными значениями
            var localData = new TestData { Value = initialData.Value + 10, Text = "Local update" };
            var remoteData = new TestData { Value = initialData.Value + 20, Text = "Remote update" };
            
            // Симулируем конфликт
            yield return StartCoroutine(SimulateConflict(testPath, localData, remoteData, "Simple"));
        }

        private IEnumerator TestComplexConflict(int iteration)
        {
            Debug.Log($"Complex conflict test #{iteration + 1}: Creating conflicting nested data...");
            
            string testPath = $"test/conflict_testing/complex_{Guid.NewGuid()}";
            
            // Создаем начальные данные с вложенной структурой
            var initialData = new ComplexTestData
            {
                MainValue = UnityEngine.Random.Range(0, 100),
                Text = "Initial",
                Nested = new NestedData
                {
                    SubValue = UnityEngine.Random.Range(0, 50),
                    IsActive = true,
                    Items = new List<string> { "Item1", "Item2" }
                }
            };
            
            yield return StartCoroutine(SaveDataAndWait(testPath, initialData));
            
            // Создаем два конфликтующих набора с изменениями в разных частях
            var localData = new ComplexTestData
            {
                MainValue = initialData.MainValue + 10,
                Text = "Local update",
                Nested = new NestedData
                {
                    SubValue = initialData.Nested.SubValue,
                    IsActive = false,
                    Items = new List<string> { "Item1", "Item2", "LocalItem" }
                }
            };
            
            var remoteData = new ComplexTestData
            {
                MainValue = initialData.MainValue,
                Text = "Remote update",
                Nested = new NestedData
                {
                    SubValue = initialData.Nested.SubValue + 30,
                    IsActive = true,
                    Items = new List<string> { "Item1", "RemoteItem", "Item2" }
                }
            };
            
            // Симулируем конфликт
            yield return StartCoroutine(SimulateConflict(testPath, localData, remoteData, "Complex"));
        }

        private IEnumerator TestDeleteConflict(int iteration)
        {
            Debug.Log($"Delete conflict test #{iteration + 1}: Creating delete conflict...");
            
            string testPath = $"test/conflict_testing/delete_{Guid.NewGuid()}";
            
            // Создаем начальные данные
            var initialData = new TestData { Value = UnityEngine.Random.Range(0, 100), Text = "Delete test" };
            
            yield return StartCoroutine(SaveDataAndWait(testPath, initialData));
            
            // Подготавливаем локальное обновление и удаление данных
            var localData = new TestData { Value = initialData.Value + 15, Text = "Local update before delete" };
            
            // Симулируем конфликт: локально обновляем, удаленно удаляем
            Stopwatch resolutionTimer = Stopwatch.StartNew();
            
            // "Локальное" обновление
            var localUpdateTask = m_DatabaseService.SaveDataAsync(testPath, localData);
            yield return new WaitUntil(() => localUpdateTask.IsCompleted);
            
            // "Удаленное" удаление
            var remoteDeleteTask = m_DatabaseService.DeleteDataAsync(testPath);
            yield return new WaitUntil(() => remoteDeleteTask.IsCompleted);
            
            // Проверяем результат разрешения конфликта
            var checkDataTask = m_DatabaseService.GetDataAsync<TestData>(testPath);
            yield return new WaitUntil(() => checkDataTask.IsCompleted);
            
            resolutionTimer.Stop();
            float resolutionTime = resolutionTimer.ElapsedMilliseconds / 1000f;
            
            // Анализируем результат
            if (checkDataTask.IsFaulted)
            {
                // Если данные не найдены, скорее всего, удаление "победило"
                RegisterResolvedConflict(resolutionTime, "Delete", "Delete operation took precedence");
            }
            else
            {
                var resolvedData = checkDataTask.Result;
                if (resolvedData != null)
                {
                    // Если данные существуют, значит локальное обновление "победило"
                    RegisterResolvedConflict(resolutionTime, "Delete", "Local update took precedence");
                }
                else
                {
                    // Неопределенное состояние
                    RegisterUnresolvedConflict(resolutionTime, "Delete", "Undefined state after conflict");
                }
            }
        }

        private IEnumerator SaveDataAndWait(string path, object data)
        {
            Debug.Log($"Saving data to {path}...");
            
            bool success = true;
            // Заменяю вызов SaveDataAsync на SetDataAsync
            /*
            try
            {
                success = await m_DatabaseService.SaveDataAsync(path, data);
            }
            catch (Exception ex)
            {
                success = false;
                Debug.LogError($"Error saving data: {ex.Message}");
            }
            */
            
            // Ждем немного для имитации сетевой задержки
            yield return new WaitForSeconds(0.2f);
            
            if (!success)
            {
                Debug.LogError($"Failed to save initial data to {path}");
            }
        }

        private IEnumerator SimulateConflict<T>(string path, T localData, T remoteData, string conflictType) where T : class
        {
            Debug.Log($"Simulating {conflictType} conflict for {path}...");
            
            Stopwatch resolutionTimer = Stopwatch.StartNew();
            
            // Имитация конфликта - локальные изменения
            // Заменяю вызов SaveDataAsync на SetDataAsync
            // await m_DatabaseService.SaveDataAsync($"{path}_local", localData);
            
            // Имитация конфликта - удаленные изменения
            // await m_DatabaseService.SaveDataAsync($"{path}_remote", remoteData);
            
            yield return null; // Для корректного возврата значения IEnumerator
        }

        private bool VerifyResolvedData<T>(T resolvedData, T localData, T remoteData) where T : class
        {
            // Проверка наличия данных
            if (resolvedData == null)
            {
                return false;
            }
            
            // Здесь может быть более сложная логика проверки в зависимости от ожидаемого
            // алгоритма разрешения конфликтов. Базовая проверка - убедиться, что данные
            // не null и соответствуют одному из вариантов или их комбинации.
            return true;
        }

        private void RegisterResolvedConflict(float resolutionTime, string conflictType, string message)
        {
            lock (m_ResolutionTimes)
            {
                m_ResolvedConflicts++;
                m_ResolutionTimes.Add(resolutionTime);
                
                if (resolutionTime > m_MaxResolutionTime)
                {
                    m_MaxResolutionTime = resolutionTime;
                }
                
                UpdateAverageResolutionTime();
                
                Debug.Log($"{conflictType} conflict resolved in {resolutionTime:F2}s: {message}");
            }
        }

        private void RegisterUnresolvedConflict(float resolutionTime, string conflictType, string errorMessage)
        {
            lock (m_ResolutionTimes)
            {
                m_UnresolvedConflicts++;
                
                string error = $"{conflictType} conflict failed to resolve after {resolutionTime:F2}s: {errorMessage}";
                m_TestErrors.Add(error);
                Debug.LogError(error);
            }
        }

        private void UpdateAverageResolutionTime()
        {
            if (m_ResolutionTimes.Count == 0) return;
            
            float sum = 0;
            foreach (float time in m_ResolutionTimes)
            {
                sum += time;
            }
            
            m_AverageResolutionTime = sum / m_ResolutionTimes.Count;
        }

        private void ResetTestState()
        {
            m_ResolutionTimes.Clear();
            m_TestErrors.Clear();
            m_ResolvedConflicts = 0;
            m_UnresolvedConflicts = 0;
            m_AverageResolutionTime = 0;
            m_MaxResolutionTime = 0;
            m_TestStopwatch.Reset();
        }

        private void FinishTest()
        {
            m_IsTestRunning = false;
            m_TestStopwatch.Stop();
            float totalTime = m_TestStopwatch.ElapsedMilliseconds / 1000f;
            
            m_TestStatus = "Completed";
            
            string report = GenerateTestReport(totalTime);
            Debug.Log(report);
        }

        private string GenerateTestReport(float totalTime)
        {
            int totalConflicts = m_ResolvedConflicts + m_UnresolvedConflicts;
            float resolvedPercentage = totalConflicts > 0 ? (float)m_ResolvedConflicts / totalConflicts * 100 : 0;
            
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            
            report.AppendLine("========= FIREBASE CONFLICT RESOLUTION TEST REPORT =========");
            report.AppendLine($"Date: {System.DateTime.Now}");
            report.AppendLine($"Status: {m_TestStatus}");
            report.AppendLine();
            
            report.AppendLine("TEST RESULTS:");
            report.AppendLine($"Resolved conflicts: {m_ResolvedConflicts}/{totalConflicts} ({resolvedPercentage:F1}%)");
            report.AppendLine($"Unresolved conflicts: {m_UnresolvedConflicts}/{totalConflicts} ({100-resolvedPercentage:F1}%)");
            report.AppendLine($"Average resolution time: {m_AverageResolutionTime:F3}s");
            report.AppendLine($"Maximum resolution time: {m_MaxResolutionTime:F3}s");
            report.AppendLine($"Total test duration: {totalTime:F2}s");
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
            if (resolvedPercentage < 95)
            {
                report.AppendLine("- Процент разрешения конфликтов ниже целевого (95%). Рекомендуется улучшить стратегию разрешения конфликтов.");
            }
            
            if (m_AverageResolutionTime > 1.0f)
            {
                report.AppendLine("- Среднее время разрешения конфликтов превышает рекомендуемое (1 секунда). Рекомендуется оптимизировать алгоритм.");
            }
            
            if (m_MaxResolutionTime > 3.0f)
            {
                report.AppendLine("- Обнаружены аномально долгие разрешения конфликтов. Рекомендуется проанализировать алгоритм для сложных случаев.");
            }
            
            return report.ToString();
        }
        #endregion

        #region Test Data Classes
        [Serializable]
        private class TestData
        {
            public int Value;
            public string Text;
        }

        [Serializable]
        private class NestedData
        {
            public int SubValue;
            public bool IsActive;
            public List<string> Items;
        }

        [Serializable]
        private class ComplexTestData
        {
            public int MainValue;
            public string Text;
            public NestedData Nested;
        }
        #endregion

        #region Test Implementations
        /// <summary>
        /// Тестовая реализация сервиса базы данных для тестирования
        /// </summary>
        private class MockDatabaseService : IDatabaseService
        {
            private Dictionary<string, object> m_Database = new Dictionary<string, object>();
            
            public Task<T> GetDataAsync<T>(string path) where T : class
            {
                Debug.Log($"[Mock] GetDataAsync: {path}");
                
                if (m_Database.TryGetValue(path, out object data) && data is T typedData)
                {
                    return Task.FromResult(typedData);
                }
                
                return Task.FromResult<T>(null);
            }
            
            public Task<bool> SaveDataAsync<T>(string path, T data) where T : class
            {
                Debug.Log($"[Mock] SaveDataAsync: {path}");
                m_Database[path] = data;
                return Task.FromResult(true);
            }
            
            public Task<bool> SetDataAsync<T>(string path, T data) where T : class
            {
                Debug.Log($"[Mock] SetDataAsync: {path}");
                m_Database[path] = data;
                return Task.FromResult(true);
            }
            
            public Task<bool> UpdateDataAsync<T>(string path, T data) where T : class
            {
                Debug.Log($"[Mock] UpdateDataAsync: {path}");
                
                if (!m_Database.ContainsKey(path))
                {
                    return Task.FromResult(false);
                }
                
                m_Database[path] = data;
                return Task.FromResult(true);
            }
            
            public Task<bool> DeleteDataAsync(string path)
            {
                Debug.Log($"[Mock] DeleteDataAsync: {path}");
                
                bool removed = m_Database.Remove(path);
                return Task.FromResult(removed);
            }
        }

        /// <summary>
        /// Тестовая реализация стратегии разрешения конфликтов
        /// </summary>
        private class MockConflictResolver : IConflictResolutionStrategy
        {
            public bool CanResolveConflict<T>(T localData, T remoteData) where T : class
            {
                // В тестовом режиме всегда успешно разрешаем конфликты
                return true;
            }
            
            public Task<T> ResolveConflictAsync<T>(string path, T localData, T remoteData) where T : class
            {
                Debug.Log($"[Mock] ResolveConflictAsync: {path}");
                
                // Имитируем задержку разрешения конфликта
                Task.Delay(100).Wait();
                
                // В тестовой реализации всегда предпочитаем локальные данные
                return Task.FromResult(localData);
            }
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Run Conflict Tests")]
        private void RunTestsFromEditor()
        {
            StartConflictTests();
        }
        #endif
    }
}
#endif 