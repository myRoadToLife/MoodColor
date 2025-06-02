using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace App.Develop.Utils.Logging
{
    /// <summary>
    /// MonoBehaviour компонент для управления логированием MyLogger через Inspector и runtime
    /// Поместите на любой GameObject на сцене для визуального управления логами
    /// </summary>
    public class LoggerController : MonoBehaviour
    {
        [Header("=== Основные настройки логирования ===")]
        [SerializeField] private bool _isDebugLoggingEnabled = true;
        [SerializeField] private bool _isWarningLoggingEnabled = true;
        [SerializeField] private bool _isErrorLoggingEnabled = true;

        [Header("=== Категории логов ===")]
        [SerializeField] private bool _defaultCategory = true;
        [SerializeField] private bool _syncCategory = false;
        [SerializeField] private bool _uiCategory = false;
        [SerializeField] private bool _networkCategory = false;
        [SerializeField] private bool _firebaseCategory = true;
        [SerializeField] private bool _editorCategory = true;
        [SerializeField] private bool _gameplayCategory = false;
        [SerializeField] private bool _bootstrapCategory = true;
        [SerializeField] private bool _emotionCategory = false;
        [SerializeField] private bool _clearHistoryCategory = false;
        [SerializeField] private bool _regionalCategory = true;
        [SerializeField] private bool _sessionCategory = false;

        [Header("=== Быстрые профили ===")]
        [Space(10)]
        [Button("Production Mode")]
        public bool _productionModeButton;

        [Button("Development Mode")]
        public bool _developmentModeButton;

        [Button("Debug Mode (All)")]
        public bool _debugModeButton;

        [Button("Firebase Debug")]
        public bool _firebaseDebugButton;

        [Button("UI Debug")]
        public bool _uiDebugButton;

        [Button("Session Debug")]
        public bool _sessionDebugButton;

        [Header("=== Runtime UI (опционально) ===")]
        [SerializeField] private GameObject _runtimePanel;
        [SerializeField] private Toggle[] _categoryToggles;
        [SerializeField] private Button _productionButton;
        [SerializeField] private Button _developmentButton;
        [SerializeField] private Button _debugButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        private bool _lastDebugEnabled;
        private bool _lastWarningEnabled;
        private bool _lastErrorEnabled;

        #region Unity Lifecycle

        private void Awake()
        {
            // Синхронизируем с текущими настройками MyLogger при старте
            SyncFromMyLogger();
        }

        private void Start()
        {
            InitializeRuntimeUI();
            ApplySettingsToMyLogger();
        }

        private void OnValidate()
        {
            // Автоматически применяем изменения из Inspector
            if (Application.isPlaying)
            {
                ApplySettingsToMyLogger();
                UpdateStatusText();
            }
        }

        private void Update()
        {
            // Отслеживаем изменения в MyLogger извне
            if (HasMyLoggerChanged())
            {
                SyncFromMyLogger();
                UpdateRuntimeUI();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Применить текущие настройки к MyLogger
        /// </summary>
        [ContextMenu("Apply Settings to MyLogger")]
        public void ApplySettingsToMyLogger()
        {
            // Основные настройки
            MyLogger.IsDebugLoggingEnabled = _isDebugLoggingEnabled;
            MyLogger.IsWarningLoggingEnabled = _isWarningLoggingEnabled;
            MyLogger.IsErrorLoggingEnabled = _isErrorLoggingEnabled;

            // Категории
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Default, _defaultCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Sync, _syncCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.UI, _uiCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Network, _networkCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Firebase, _firebaseCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Editor, _editorCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Gameplay, _gameplayCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Bootstrap, _bootstrapCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Emotion, _emotionCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.ClearHistory, _clearHistoryCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Regional, _regionalCategory);
            MyLogger.SetCategoryEnabled(MyLogger.LogCategory.Session, _sessionCategory);

            UpdateLastKnownState();
            UpdateStatusText();

            MyLogger.Log("LoggerController: Настройки применены", MyLogger.LogCategory.Bootstrap);
        }

        /// <summary>
        /// Синхронизировать с текущими настройками MyLogger
        /// </summary>
        [ContextMenu("Sync from MyLogger")]
        public void SyncFromMyLogger()
        {
            // Основные настройки
            _isDebugLoggingEnabled = MyLogger.IsDebugLoggingEnabled;
            _isWarningLoggingEnabled = MyLogger.IsWarningLoggingEnabled;
            _isErrorLoggingEnabled = MyLogger.IsErrorLoggingEnabled;

            // Категории
            _defaultCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Default);
            _syncCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Sync);
            _uiCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.UI);
            _networkCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Network);
            _firebaseCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Firebase);
            _editorCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Editor);
            _gameplayCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Gameplay);
            _bootstrapCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Bootstrap);
            _emotionCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Emotion);
            _clearHistoryCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.ClearHistory);
            _regionalCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Regional);
            _sessionCategory = MyLogger.IsCategoryEnabled(MyLogger.LogCategory.Session);

            UpdateLastKnownState();
            UpdateStatusText();
        }

        #endregion

        #region Profile Methods

        [ContextMenu("Set Production Mode")]
        public void SetProductionMode()
        {
            MyLogger.SetProductionMode();
            SyncFromMyLogger();
            UpdateRuntimeUI();
            MyLogger.Log("LoggerController: Production Mode активирован", MyLogger.LogCategory.Bootstrap);
        }

        [ContextMenu("Set Development Mode")]
        public void SetDevelopmentMode()
        {
            MyLogger.SetDevelopmentMode();
            SyncFromMyLogger();
            UpdateRuntimeUI();
            MyLogger.Log("LoggerController: Development Mode активирован", MyLogger.LogCategory.Bootstrap);
        }

        [ContextMenu("Set Debug Mode")]
        public void SetDebugMode()
        {
            MyLogger.SetDebugMode();
            SyncFromMyLogger();
            UpdateRuntimeUI();
            MyLogger.Log("LoggerController: Debug Mode (все логи) активирован", MyLogger.LogCategory.Bootstrap);
        }

        [ContextMenu("Enable Firebase Debug")]
        public void EnableFirebaseDebugMode()
        {
            MyLogger.EnableFirebaseDebugMode();
            SyncFromMyLogger();
            UpdateRuntimeUI();
            MyLogger.Log("LoggerController: Firebase Debug Mode активирован", MyLogger.LogCategory.Bootstrap);
        }

        [ContextMenu("Enable UI Debug")]
        public void EnableUIDebugMode()
        {
            MyLogger.EnableUIDebugMode();
            SyncFromMyLogger();
            UpdateRuntimeUI();
            MyLogger.Log("LoggerController: UI Debug Mode активирован", MyLogger.LogCategory.Bootstrap);
        }

        [ContextMenu("Enable Session Debug")]
        public void EnableSessionDebugMode()
        {
            MyLogger.EnableSessionDebugMode();
            SyncFromMyLogger();
            UpdateRuntimeUI();
            MyLogger.Log("LoggerController: Session Debug Mode активирован", MyLogger.LogCategory.Bootstrap);
        }

        #endregion

        #region Runtime UI

        private void InitializeRuntimeUI()
        {
            if (_productionButton != null)
                _productionButton.onClick.AddListener(SetProductionMode);

            if (_developmentButton != null)
                _developmentButton.onClick.AddListener(SetDevelopmentMode);

            if (_debugButton != null)
                _debugButton.onClick.AddListener(SetDebugMode);

            // Инициализация тогглов категорий
            if (_categoryToggles != null && _categoryToggles.Length >= 12)
            {
                _categoryToggles[0].onValueChanged.AddListener(value => { _defaultCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[1].onValueChanged.AddListener(value => { _syncCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[2].onValueChanged.AddListener(value => { _uiCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[3].onValueChanged.AddListener(value => { _networkCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[4].onValueChanged.AddListener(value => { _firebaseCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[5].onValueChanged.AddListener(value => { _editorCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[6].onValueChanged.AddListener(value => { _gameplayCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[7].onValueChanged.AddListener(value => { _bootstrapCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[8].onValueChanged.AddListener(value => { _emotionCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[9].onValueChanged.AddListener(value => { _clearHistoryCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[10].onValueChanged.AddListener(value => { _regionalCategory = value; ApplySettingsToMyLogger(); });
                _categoryToggles[11].onValueChanged.AddListener(value => { _sessionCategory = value; ApplySettingsToMyLogger(); });
            }

            UpdateRuntimeUI();
        }

        private void UpdateRuntimeUI()
        {
            if (_categoryToggles != null && _categoryToggles.Length >= 12)
            {
                _categoryToggles[0].SetIsOnWithoutNotify(_defaultCategory);
                _categoryToggles[1].SetIsOnWithoutNotify(_syncCategory);
                _categoryToggles[2].SetIsOnWithoutNotify(_uiCategory);
                _categoryToggles[3].SetIsOnWithoutNotify(_networkCategory);
                _categoryToggles[4].SetIsOnWithoutNotify(_firebaseCategory);
                _categoryToggles[5].SetIsOnWithoutNotify(_editorCategory);
                _categoryToggles[6].SetIsOnWithoutNotify(_gameplayCategory);
                _categoryToggles[7].SetIsOnWithoutNotify(_bootstrapCategory);
                _categoryToggles[8].SetIsOnWithoutNotify(_emotionCategory);
                _categoryToggles[9].SetIsOnWithoutNotify(_clearHistoryCategory);
                _categoryToggles[10].SetIsOnWithoutNotify(_regionalCategory);
                _categoryToggles[11].SetIsOnWithoutNotify(_sessionCategory);
            }

            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (_statusText == null) return;

            int enabledCount = GetEnabledCategoriesCount();
            string status = $"Логи: {(_isDebugLoggingEnabled ? "ВКЛ" : "ВЫКЛ")} | " +
                           $"Предупреждения: {(_isWarningLoggingEnabled ? "ВКЛ" : "ВЫКЛ")} | " +
                           $"Ошибки: {(_isErrorLoggingEnabled ? "ВКЛ" : "ВЫКЛ")}\n" +
                           $"Активных категорий: {enabledCount}/12";

            _statusText.text = status;
        }

        #endregion

        #region Helper Methods

        private bool HasMyLoggerChanged()
        {
            return _lastDebugEnabled != MyLogger.IsDebugLoggingEnabled ||
                   _lastWarningEnabled != MyLogger.IsWarningLoggingEnabled ||
                   _lastErrorEnabled != MyLogger.IsErrorLoggingEnabled;
        }

        private void UpdateLastKnownState()
        {
            _lastDebugEnabled = MyLogger.IsDebugLoggingEnabled;
            _lastWarningEnabled = MyLogger.IsWarningLoggingEnabled;
            _lastErrorEnabled = MyLogger.IsErrorLoggingEnabled;
        }

        private int GetEnabledCategoriesCount()
        {
            int count = 0;
            if (_defaultCategory) count++;
            if (_syncCategory) count++;
            if (_uiCategory) count++;
            if (_networkCategory) count++;
            if (_firebaseCategory) count++;
            if (_editorCategory) count++;
            if (_gameplayCategory) count++;
            if (_bootstrapCategory) count++;
            if (_emotionCategory) count++;
            if (_clearHistoryCategory) count++;
            if (_regionalCategory) count++;
            if (_sessionCategory) count++;
            return count;
        }

        #endregion

        #region Test Methods

        [ContextMenu("Test All Categories")]
        public void TestAllCategories()
        {
            MyLogger.Log("Тест Default категории", MyLogger.LogCategory.Default);
            MyLogger.Log("Тест Sync категории", MyLogger.LogCategory.Sync);
            MyLogger.Log("Тест UI категории", MyLogger.LogCategory.UI);
            MyLogger.Log("Тест Network категории", MyLogger.LogCategory.Network);
            MyLogger.Log("Тест Firebase категории", MyLogger.LogCategory.Firebase);
            MyLogger.Log("Тест Editor категории", MyLogger.LogCategory.Editor);
            MyLogger.Log("Тест Gameplay категории", MyLogger.LogCategory.Gameplay);
            MyLogger.Log("Тест Bootstrap категории", MyLogger.LogCategory.Bootstrap);
            MyLogger.Log("Тест Emotion категории", MyLogger.LogCategory.Emotion);
            MyLogger.Log("Тест ClearHistory категории", MyLogger.LogCategory.ClearHistory);
            MyLogger.Log("Тест Regional категории", MyLogger.LogCategory.Regional);
            MyLogger.Log("Тест Session категории", MyLogger.LogCategory.Session);
        }

        [ContextMenu("Test Warning and Error")]
        public void TestWarningAndError()
        {
            MyLogger.LogWarning("Тестовое предупреждение", MyLogger.LogCategory.Default);
            MyLogger.LogError("Тестовая ошибка", MyLogger.LogCategory.Default);
        }

        #endregion
    }

    #region Custom Attribute для кнопок в Inspector

    /// <summary>
    /// Атрибут для отображения кнопки в Inspector
    /// </summary>
    public class ButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }

        public ButtonAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }

    #endregion
}