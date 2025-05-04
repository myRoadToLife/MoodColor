#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Класс для тестирования ConnectionManager
    /// </summary>
    public class ConnectionManagerTests : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Test Configuration")]
        [SerializeField] private bool m_RunOnStart = false;
        [SerializeField] private int m_ConnectionSwitchCount = 20;
        [SerializeField] private float m_DelayBetweenTests = 1.0f;
        
        [Header("Test Results")]
        [SerializeField] private string m_TestStatus = "Ready";
        [SerializeField] private float m_AverageReconnectTime;
        [SerializeField] private float m_MaxReconnectTime;
        [SerializeField] private int m_SuccessfulReconnects;
        [SerializeField] private int m_FailedReconnects;
        #endregion

        #region Private Fields
        private IFirebaseConnectionManager m_ConnectionManager;
        
        private readonly List<float> m_ReconnectTimes = new List<float>();
        private readonly List<string> m_TestErrors = new List<string>();
        
        private bool m_IsTestRunning;
        private Stopwatch m_TestStopwatch = new Stopwatch();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Получение менеджера соединения
            m_ConnectionManager = ServiceLocator.Get<IFirebaseConnectionManager>();
            
            if (m_ConnectionManager == null)
            {
                Debug.LogError("ConnectionManager not available. Please ensure it is registered in the ServiceLocator.");
            }
            
            if (m_RunOnStart)
            {
                StartConnectionTests();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Запускает тесты соединения
        /// </summary>
        public void StartConnectionTests()
        {
            if (m_IsTestRunning)
            {
                Debug.LogWarning("Connection tests already running!");
                return;
            }
            
            if (m_ConnectionManager == null)
            {
                Debug.LogError("ConnectionManager not available!");
                return;
            }
            
            StartCoroutine(RunConnectionTests());
        }
        
        /// <summary>
        /// Прерывает текущие тесты
        /// </summary>
        public void AbortTests()
        {
            if (!m_IsTestRunning) return;
            
            m_IsTestRunning = false;
            m_TestStatus = "Aborted";
            
            Debug.Log("Connection tests aborted by user");
        }
        #endregion

        #region Private Methods
        private IEnumerator RunConnectionTests()
        {
            // Сброс результатов предыдущего теста
            ResetTestState();
            
            m_IsTestRunning = true;
            m_TestStopwatch.Start();
            m_TestStatus = "Running";
            
            Debug.Log($"Starting Firebase connection tests with {m_ConnectionSwitchCount} reconnects");
            
            // Проверяем состояние соединения
            bool isConnected = m_ConnectionManager.IsConnected;
            Debug.Log($"Initial connection state: {(isConnected ? "Connected" : "Disconnected")}");
            
            // Если соединение отсутствует, пытаемся установить его
            if (!isConnected)
            {
                yield return StartCoroutine(TestReconnect("Initial"));
            }
            
            // Основной цикл тестов
            for (int i = 0; i < m_ConnectionSwitchCount; i++)
            {
                if (!m_IsTestRunning) break;
                
                // Отключаем и подключаем
                yield return StartCoroutine(TestDisconnectReconnect(i));
                
                // Пауза между тестами
                yield return new WaitForSeconds(m_DelayBetweenTests);
            }
            
            FinishTest();
        }

        private IEnumerator TestDisconnectReconnect(int iteration)
        {
            Debug.Log($"Test #{iteration + 1}: Disconnecting from Firebase...");
            
            // Отключаемся
            m_ConnectionManager.ForceDisconnect();
            
            // Проверяем, что действительно отключились
            float disconnectTimeout = 5.0f;
            float elapsed = 0f;
            
            while (m_ConnectionManager.IsConnected && elapsed < disconnectTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (m_ConnectionManager.IsConnected)
            {
                string error = $"Test #{iteration + 1}: Failed to disconnect after {disconnectTimeout} seconds";
                m_TestErrors.Add(error);
                Debug.LogError(error);
            }
            else
            {
                Debug.Log($"Test #{iteration + 1}: Successfully disconnected");
            }
            
            // Проверяем переподключение
            yield return StartCoroutine(TestReconnect($"#{iteration + 1}"));
        }

        private IEnumerator TestReconnect(string testId)
        {
            Debug.Log($"Test {testId}: Reconnecting to Firebase...");
            
            Stopwatch reconnectTimer = Stopwatch.StartNew();
            
            // Запускаем переподключение
            m_ConnectionManager.ForceReconnect();
            
            // Проверяем, что действительно подключились
            float reconnectTimeout = 10.0f;  // Таймаут подключения
            float elapsed = 0f;
            
            while (!m_ConnectionManager.IsConnected && elapsed < reconnectTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            reconnectTimer.Stop();
            float reconnectTime = reconnectTimer.ElapsedMilliseconds / 1000f;
            
            if (m_ConnectionManager.IsConnected)
            {
                RegisterSuccessfulReconnect(reconnectTime, testId);
            }
            else
            {
                RegisterFailedReconnect(reconnectTime, testId);
            }
        }

        private void RegisterSuccessfulReconnect(float reconnectTime, string testId)
        {
            lock (m_ReconnectTimes)
            {
                m_SuccessfulReconnects++;
                m_ReconnectTimes.Add(reconnectTime);
                
                if (reconnectTime > m_MaxReconnectTime)
                {
                    m_MaxReconnectTime = reconnectTime;
                }
                
                UpdateAverageReconnectTime();
                
                Debug.Log($"Test {testId}: Successfully reconnected in {reconnectTime:F2}s");
            }
        }

        private void RegisterFailedReconnect(float reconnectTime, string testId)
        {
            lock (m_ReconnectTimes)
            {
                m_FailedReconnects++;
                
                string error = $"Test {testId}: Failed to reconnect after {reconnectTime:F2}s";
                m_TestErrors.Add(error);
                Debug.LogError(error);
            }
        }

        private void UpdateAverageReconnectTime()
        {
            if (m_ReconnectTimes.Count == 0) return;
            
            float sum = 0;
            foreach (float time in m_ReconnectTimes)
            {
                sum += time;
            }
            
            m_AverageReconnectTime = sum / m_ReconnectTimes.Count;
        }

        private void ResetTestState()
        {
            m_ReconnectTimes.Clear();
            m_TestErrors.Clear();
            m_SuccessfulReconnects = 0;
            m_FailedReconnects = 0;
            m_AverageReconnectTime = 0;
            m_MaxReconnectTime = 0;
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
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            
            report.AppendLine("========= FIREBASE CONNECTION TEST REPORT =========");
            report.AppendLine($"Date: {System.DateTime.Now}");
            report.AppendLine($"Status: {m_TestStatus}");
            report.AppendLine();
            
            report.AppendLine("TEST RESULTS:");
            report.AppendLine($"Successful reconnects: {m_SuccessfulReconnects}/{m_ConnectionSwitchCount} ({(float)m_SuccessfulReconnects/m_ConnectionSwitchCount*100:F1}%)");
            report.AppendLine($"Failed reconnects: {m_FailedReconnects}/{m_ConnectionSwitchCount} ({(float)m_FailedReconnects/m_ConnectionSwitchCount*100:F1}%)");
            report.AppendLine($"Average reconnect time: {m_AverageReconnectTime:F3}s");
            report.AppendLine($"Maximum reconnect time: {m_MaxReconnectTime:F3}s");
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
            if (m_FailedReconnects > 0)
            {
                report.AppendLine("- Обнаружены ошибки переподключения. Рекомендуется улучшить механизм обработки обрывов соединения.");
            }
            
            if (m_AverageReconnectTime > 2.0f)
            {
                report.AppendLine("- Среднее время переподключения превышает рекомендуемое (2 секунды). Рекомендуется оптимизировать процесс установления соединения.");
            }
            
            if (m_MaxReconnectTime > 5.0f)
            {
                report.AppendLine("- Обнаружены аномально долгие переподключения. Рекомендуется реализовать таймауты для операций.");
            }
            
            return report.ToString();
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Run Connection Tests")]
        private void RunTestsFromEditor()
        {
            StartConnectionTests();
        }
        #endif
    }
}
#endif 