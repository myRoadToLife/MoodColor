#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Хранение, анализ и экспорт результатов нагрузочных тестов Firebase
    /// </summary>
    public class FirebaseLoadTestResults : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private FirebaseLoadTestManager m_TestManager;
        [SerializeField] private bool m_AutoSaveResults = true;
        [SerializeField] private bool m_SaveResultsToFirebase = true;
        [SerializeField] private string m_FirebaseResultsPath = "system/load_test_results";
        #endregion

        #region Private Fields
        private IFirebaseDatabaseService m_DatabaseService;
        private readonly List<TestResult> m_TestHistory = new List<TestResult>();
        #endregion

        #region Classes
        [Serializable]
        public class TestResult
        {
            public string Date;
            public string DeviceModel;
            public string SystemInfo;
            public int SuccessfulOperations;
            public int FailedOperations;
            public float AverageResponseTime;
            public float MaxResponseTime;
            public float TotalDuration;
            public int TotalOperations;
            public int ConcurrentOperations;
            public bool TestReads;
            public bool TestWrites;
            public bool TestDeletes;
            public bool TestRealTimeUpdates;
            public string TestStatus;
            public List<string> Errors;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Получаем сервис базы данных
            m_DatabaseService = ServiceLocator.Get<IFirebaseDatabaseService>();
            
            if (m_DatabaseService == null)
            {
                Debug.LogWarning("Firebase database service not available. Results won't be saved to Firebase.");
            }
            
            if (m_TestManager == null)
            {
                m_TestManager = FindObjectOfType<FirebaseLoadTestManager>();
            }
        }

        private void Start()
        {
            // Подписываемся на событие завершения теста через рефлексию, если такая возможность имеется
            // Или можно добавить такое событие в FirebaseLoadTestManager
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Собирает и сохраняет результаты тестирования
        /// </summary>
        public void SaveTestResults()
        {
            if (m_TestManager == null) return;
            
            var testResult = CollectTestResult();
            
            if (testResult == null) return;
            
            // Сохраняем в историю
            m_TestHistory.Add(testResult);
            
            // Сохраняем на устройстве
            SaveResultToDevice(testResult);
            
            // Сохраняем в Firebase, если включено
            if (m_SaveResultsToFirebase && m_DatabaseService != null)
            {
                SaveResultToFirebase(testResult);
            }
        }

        /// <summary>
        /// Экспортирует историю тестирования в файл
        /// </summary>
        public void ExportTestHistory()
        {
            if (m_TestHistory.Count == 0)
            {
                Debug.LogWarning("No test history to export");
                return;
            }
            
            string path = $"{Application.persistentDataPath}/firebase_load_test_history_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            try
            {
                string json = JsonUtility.ToJson(new { results = m_TestHistory }, true);
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Test history exported to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export test history: {e.Message}");
            }
        }
        #endregion

        #region Private Methods
        private TestResult CollectTestResult()
        {
            // Получаем данные из FirebaseLoadTestManager через рефлексию
            var testStatusField = m_TestManager.GetType().GetField("m_TestStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var successfulOpsField = m_TestManager.GetType().GetField("m_SuccessfulOperations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var failedOpsField = m_TestManager.GetType().GetField("m_FailedOperations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var avgTimeField = m_TestManager.GetType().GetField("m_AverageResponseTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxTimeField = m_TestManager.GetType().GetField("m_MaxResponseTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testStopwatchField = m_TestManager.GetType().GetField("m_TestStopwatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var totalOpsField = m_TestManager.GetType().GetField("m_TotalOperationsCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var concurrentOpsField = m_TestManager.GetType().GetField("m_ConcurrentOperationsCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testReadsField = m_TestManager.GetType().GetField("m_TestReads", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testWritesField = m_TestManager.GetType().GetField("m_TestWrites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testDeletesField = m_TestManager.GetType().GetField("m_TestDeletes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testRealTimeField = m_TestManager.GetType().GetField("m_TestRealTimeUpdates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testErrorsField = m_TestManager.GetType().GetField("m_TestErrors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (testStatusField == null || successfulOpsField == null || failedOpsField == null || 
                avgTimeField == null || maxTimeField == null || testStopwatchField == null || 
                totalOpsField == null || concurrentOpsField == null || testReadsField == null || 
                testWritesField == null || testDeletesField == null || testRealTimeField == null ||
                testErrorsField == null)
            {
                Debug.LogError("Field reflection failed. Make sure the field names match the TestManager implementation.");
                return null;
            }
            
            // Получаем Stopwatch для расчета общего времени
            var stopwatch = testStopwatchField.GetValue(m_TestManager) as System.Diagnostics.Stopwatch;
            if (stopwatch == null) return null;
            
            // Получаем список ошибок
            var errors = testErrorsField.GetValue(m_TestManager) as List<string>;
            
            // Создаем и заполняем результат тестирования
            TestResult result = new TestResult
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DeviceModel = SystemInfo.deviceModel,
                SystemInfo = $"{SystemInfo.operatingSystem}, {SystemInfo.processorType}, RAM: {SystemInfo.systemMemorySize}MB",
                SuccessfulOperations = (int)successfulOpsField.GetValue(m_TestManager),
                FailedOperations = (int)failedOpsField.GetValue(m_TestManager),
                AverageResponseTime = (float)avgTimeField.GetValue(m_TestManager),
                MaxResponseTime = (float)maxTimeField.GetValue(m_TestManager),
                TotalDuration = stopwatch.ElapsedMilliseconds / 1000f,
                TotalOperations = (int)totalOpsField.GetValue(m_TestManager),
                ConcurrentOperations = (int)concurrentOpsField.GetValue(m_TestManager),
                TestReads = (bool)testReadsField.GetValue(m_TestManager),
                TestWrites = (bool)testWritesField.GetValue(m_TestManager),
                TestDeletes = (bool)testDeletesField.GetValue(m_TestManager),
                TestRealTimeUpdates = (bool)testRealTimeField.GetValue(m_TestManager),
                TestStatus = (string)testStatusField.GetValue(m_TestManager),
                Errors = errors != null ? new List<string>(errors) : new List<string>()
            };
            
            return result;
        }

        private void SaveResultToDevice(TestResult result)
        {
            string path = $"{Application.persistentDataPath}/firebase_load_test_result_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            try
            {
                string json = JsonUtility.ToJson(result, true);
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Test result saved to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save test result: {e.Message}");
            }
        }

        private void SaveResultToFirebase(TestResult result)
        {
            if (m_DatabaseService == null) return;
            
            string path = $"{m_FirebaseResultsPath}/{DateTime.Now:yyyyMMddHHmmss}";
            Debug.Log($"Saving test result to Firebase path: {path}");
            
            m_DatabaseService.SaveDataAsync(path, result)
                .ContinueWith(task => 
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Failed to save test result to Firebase: {task.Exception?.Message}");
                    }
                    else
                    {
                        Debug.Log("Test result saved to Firebase successfully");
                    }
                });
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Можно вызвать через меню
        /// </summary>
        public void OnTestCompleted()
        {
            if (m_AutoSaveResults)
            {
                SaveTestResults();
            }
        }
        #endregion
    }
} 
#endif 