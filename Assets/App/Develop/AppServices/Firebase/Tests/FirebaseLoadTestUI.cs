#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// UI для управления нагрузочными тестами Firebase
    /// </summary>
    public class FirebaseLoadTestUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private FirebaseLoadTestManager m_TestManager;
        
        [Header("UI Controls")]
        [SerializeField] private Button m_StartTestButton;
        [SerializeField] private Button m_AbortTestButton;
        [SerializeField] private Button m_ExportResultsButton;
        [SerializeField] private Slider m_ProgressSlider;
        
        [Header("UI Text")]
        [SerializeField] private TextMeshProUGUI m_StatusText;
        [SerializeField] private TextMeshProUGUI m_ResultsText;
        #endregion

        #region Private Fields
        private bool m_IsTestRunning;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (m_TestManager == null)
            {
                m_TestManager = FindObjectOfType<FirebaseLoadTestManager>();
                if (m_TestManager == null)
                {
                    Debug.LogError("TestManager not found. Please assign it in the inspector.");
                    return;
                }
            }
            
            SetupUI();
        }

        private void Update()
        {
            if (m_TestManager == null) return;
            
            // Обновляем состояние UI
            UpdateUIState();
        }
        #endregion

        #region Private Methods
        private void SetupUI()
        {
            // Устанавливаем обработчики событий кнопок
            if (m_StartTestButton != null)
            {
                m_StartTestButton.onClick.AddListener(OnStartTestClick);
            }
            
            if (m_AbortTestButton != null)
            {
                m_AbortTestButton.onClick.AddListener(OnAbortTestClick);
            }
            
            if (m_ExportResultsButton != null)
            {
                m_ExportResultsButton.onClick.AddListener(OnExportResultsClick);
            }
            
            // Начальная настройка UI
            if (m_AbortTestButton != null)
            {
                m_AbortTestButton.interactable = false;
            }
            
            if (m_ExportResultsButton != null)
            {
                m_ExportResultsButton.interactable = false;
            }
            
            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.value = 0;
            }
            
            UpdateStatusText("Ready to start test");
        }

        private void UpdateUIState()
        {
            // Получаем данные из FirebaseLoadTestManager через рефлексию
            var testStatusField = m_TestManager.GetType().GetField("m_TestStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressField = m_TestManager.GetType().GetField("m_TestProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var successfulOpsField = m_TestManager.GetType().GetField("m_SuccessfulOperations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var failedOpsField = m_TestManager.GetType().GetField("m_FailedOperations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var avgTimeField = m_TestManager.GetType().GetField("m_AverageResponseTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxTimeField = m_TestManager.GetType().GetField("m_MaxResponseTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isRunningField = m_TestManager.GetType().GetField("m_IsTestRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (testStatusField == null || progressField == null || successfulOpsField == null || 
                failedOpsField == null || avgTimeField == null || maxTimeField == null || isRunningField == null)
            {
                Debug.LogError("Field reflection failed. Make sure the field names match the TestManager implementation.");
                return;
            }
            
            // Получаем значения полей
            string status = (string)testStatusField.GetValue(m_TestManager);
            float progress = (float)progressField.GetValue(m_TestManager);
            int successfulOps = (int)successfulOpsField.GetValue(m_TestManager);
            int failedOps = (int)failedOpsField.GetValue(m_TestManager);
            float avgTime = (float)avgTimeField.GetValue(m_TestManager);
            float maxTime = (float)maxTimeField.GetValue(m_TestManager);
            bool isRunning = (bool)isRunningField.GetValue(m_TestManager);
            
            // Обновляем UI компоненты
            UpdateStatusText(status);
            
            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.value = progress;
            }
            
            UpdateResultsText(successfulOps, failedOps, avgTime, maxTime);
            
            // Обновляем состояние кнопок
            if (m_StartTestButton != null)
            {
                m_StartTestButton.interactable = !isRunning;
            }
            
            if (m_AbortTestButton != null)
            {
                m_AbortTestButton.interactable = isRunning;
            }
            
            if (m_ExportResultsButton != null)
            {
                m_ExportResultsButton.interactable = !isRunning && (successfulOps > 0 || failedOps > 0);
            }
            
            m_IsTestRunning = isRunning;
        }

        private void UpdateStatusText(string status)
        {
            if (m_StatusText != null)
            {
                m_StatusText.text = $"Status: {status}";
            }
        }

        private void UpdateResultsText(int successfulOps, int failedOps, float avgTime, float maxTime)
        {
            if (m_ResultsText == null) return;
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Successful: {successfulOps}");
            sb.AppendLine($"Failed: {failedOps}");
            sb.AppendLine($"Average response time: {avgTime:F3}s");
            sb.AppendLine($"Maximum response time: {maxTime:F3}s");
            
            m_ResultsText.text = sb.ToString();
        }
        #endregion

        #region Button Event Handlers
        private void OnStartTestClick()
        {
            if (m_TestManager == null) return;
            
            m_TestManager.StartLoadTest();
        }

        private void OnAbortTestClick()
        {
            if (m_TestManager == null) return;
            
            m_TestManager.AbortTest();
        }

        private void OnExportResultsClick()
        {
            if (m_TestManager == null) return;
            
            m_TestManager.ExportResults();
        }
        #endregion
    }
}
#endif 