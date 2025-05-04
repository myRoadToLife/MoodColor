using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.CommonServices.Firebase.Database.UI
{
    /// <summary>
    /// UI для разрешения конфликтов синхронизации данных
    /// </summary>
    public class ConflictResolutionUI : MonoBehaviour, IInjectable
    {
        #region UI элементы
        
        [Header("Основные элементы")]
        [SerializeField] private GameObject _conflictPanel; // Корневая панель для разрешения конфликтов
        [SerializeField] private TextMeshProUGUI _titleText; // Заголовок панели
        [SerializeField] private TextMeshProUGUI _descriptionText; // Описание конфликта
        
        [Header("Данные эмоций")]
        [SerializeField] private GameObject _emotionConflictPanel; // Панель для конфликтов эмоций
        [SerializeField] private TextMeshProUGUI _emotionTypeText; // Тип эмоции
        [SerializeField] private Image _emotionColorImage; // Цвет эмоции
        
        [Header("Локальные данные")]
        [SerializeField] private GameObject _localDataContainer; // Контейнер для локальных данных 
        [SerializeField] private TextMeshProUGUI _localDataTimestampText; // Время локальных данных
        [SerializeField] private TextMeshProUGUI _localDataValueText; // Значение локальных данных
        [SerializeField] private TextMeshProUGUI _localDataNoteText; // Заметка локальных данных
        [SerializeField] private Button _useLocalDataButton; // Кнопка выбора локальных данных
        
        [Header("Серверные данные")]
        [SerializeField] private GameObject _serverDataContainer; // Контейнер для серверных данных
        [SerializeField] private TextMeshProUGUI _serverDataTimestampText; // Время серверных данных
        [SerializeField] private TextMeshProUGUI _serverDataValueText; // Значение серверных данных
        [SerializeField] private TextMeshProUGUI _serverDataNoteText; // Заметка серверных данных
        [SerializeField] private Button _useServerDataButton; // Кнопка выбора серверных данных
        
        [Header("Дополнительные опции")]
        [SerializeField] private Button _mergeDatasButton; // Кнопка для слияния данных
        [SerializeField] private Button _cancelButton; // Кнопка отмены
        [SerializeField] private Toggle _rememberChoiceToggle; // Запомнить мой выбор
        
        #endregion
        
        #region Приватные поля
        
        private ConflictResolutionManager _conflictManager;
        
        private TaskCompletionSource<EmotionData> _currentResolutionTask;
        private EmotionData _localEmotionData;
        private EmotionData _serverEmotionData;
        private bool _isProcessing = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Скрываем панели по умолчанию
            if (_conflictPanel != null)
                _conflictPanel.SetActive(false);
                
            if (_emotionConflictPanel != null) 
                _emotionConflictPanel.SetActive(false);
        }
        
        private void OnEnable()
        {
            // Подписываемся на события кнопок
            if (_useLocalDataButton != null)
                _useLocalDataButton.onClick.AddListener(OnUseLocalDataClicked);
                
            if (_useServerDataButton != null) 
                _useServerDataButton.onClick.AddListener(OnUseServerDataClicked);
                
            if (_mergeDatasButton != null)
                _mergeDatasButton.onClick.AddListener(OnMergeDatasClicked);
                
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        private void OnDisable()
        {
            // Отписываемся от событий кнопок
            if (_useLocalDataButton != null)
                _useLocalDataButton.onClick.RemoveListener(OnUseLocalDataClicked);
                
            if (_useServerDataButton != null)
                _useServerDataButton.onClick.RemoveListener(OnUseServerDataClicked);
                
            if (_mergeDatasButton != null)
                _mergeDatasButton.onClick.RemoveListener(OnMergeDatasClicked);
                
            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
        
        #endregion
        
        #region Инициализация
        
        public void Inject(DIContainer container)
        {
            try
            {
                _conflictManager = container.Resolve<ConflictResolutionManager>();
                
                // Подписываемся на события конфликт-менеджера
                if (_conflictManager != null)
                {
                    _conflictManager.OnManualResolutionRequiredEmotions += ShowEmotionConflictResolution;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при инициализации UI разрешения конфликтов: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Публичные методы
        
        /// <summary>
        /// Показать диалог разрешения конфликта эмоций и вернуть выбранный вариант
        /// </summary>
        /// <param name="localData">Локальные данные</param>
        /// <param name="serverData">Данные с сервера</param>
        /// <returns>Выбранные пользователем данные</returns>
        public async Task<EmotionData> ShowConflictResolutionDialogAsync(EmotionData localData, EmotionData serverData)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("Диалог разрешения конфликтов уже открыт");
                return null;
            }
            
            _isProcessing = true;
            
            // Создаем новый TaskCompletionSource для ожидания выбора пользователя
            _currentResolutionTask = new TaskCompletionSource<EmotionData>();
            
            // Показываем диалог с данными
            ShowEmotionConflictResolution(localData, serverData, null);
            
            // Ждем ответа пользователя
            EmotionData result = await _currentResolutionTask.Task;
            
            // Скрываем диалог
            HideConflictPanel();
            
            _isProcessing = false;
            
            return result;
        }
        
        #endregion
        
        #region Приватные методы обработки событий
        
        /// <summary>
        /// Показывает диалог разрешения конфликта эмоций
        /// </summary>
        private void ShowEmotionConflictResolution(EmotionData localData, EmotionData serverData, Action<EmotionData> callback)
        {
            // Сохраняем данные
            _localEmotionData = localData;
            _serverEmotionData = serverData;
            
            // Если диалог вызван через событие, создаем задачу
            if (callback != null && _currentResolutionTask == null)
            {
                _currentResolutionTask = new TaskCompletionSource<EmotionData>();
                
                // Сохраняем callback для вызова после выбора пользователя
                _currentResolutionTask.Task.ContinueWith(t => {
                    if (t.IsCompleted && !t.IsFaulted)
                    {
                        callback(t.Result);
                    }
                });
            }
            
            // Показываем панель конфликта
            if (_conflictPanel != null)
                _conflictPanel.SetActive(true);
                
            if (_emotionConflictPanel != null)
                _emotionConflictPanel.SetActive(true);
                
            // Заполняем информацию
            if (_titleText != null)
                _titleText.text = "Разрешение конфликта данных";
                
            if (_descriptionText != null)
                _descriptionText.text = "Данные были изменены как локально, так и на сервере. Выберите, какие данные сохранить.";
                
            if (_emotionTypeText != null)
                _emotionTypeText.text = $"Эмоция: {localData.Type}";
                
            // Установка цвета эмоции
            if (_emotionColorImage != null && !string.IsNullOrEmpty(localData.ColorHex))
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(localData.ColorHex, out color))
                {
                    _emotionColorImage.color = color;
                }
            }
            
            // Заполняем информацию о локальных данных
            if (_localDataTimestampText != null)
                _localDataTimestampText.text = $"Дата: {DateTime.FromFileTimeUtc(localData.Timestamp).ToString("dd.MM.yyyy HH:mm")}";
                
            if (_localDataValueText != null)
                _localDataValueText.text = $"Значение: {localData.Value:F1} / Интенсивность: {localData.Intensity:F1}";
                
            if (_localDataNoteText != null)
                _localDataNoteText.text = string.IsNullOrEmpty(localData.Note) ? "Без заметки" : localData.Note;
                
            // Заполняем информацию о серверных данных
            if (_serverDataTimestampText != null)
                _serverDataTimestampText.text = $"Дата: {DateTime.FromFileTimeUtc(serverData.Timestamp).ToString("dd.MM.yyyy HH:mm")}";
                
            if (_serverDataValueText != null)
                _serverDataValueText.text = $"Значение: {serverData.Value:F1} / Интенсивность: {serverData.Intensity:F1}";
                
            if (_serverDataNoteText != null)
                _serverDataNoteText.text = string.IsNullOrEmpty(serverData.Note) ? "Без заметки" : serverData.Note;
                
            // Включаем кнопку слияния только если есть заметки для слияния
            if (_mergeDatasButton != null)
            {
                bool canMerge = !string.IsNullOrEmpty(localData.Note) && !string.IsNullOrEmpty(serverData.Note) 
                    && localData.Note != serverData.Note;
                _mergeDatasButton.gameObject.SetActive(canMerge);
            }
        }
        
        /// <summary>
        /// Скрывает все панели конфликтов
        /// </summary>
        private void HideConflictPanel()
        {
            if (_conflictPanel != null)
                _conflictPanel.SetActive(false);
                
            if (_emotionConflictPanel != null)
                _emotionConflictPanel.SetActive(false);
        }
        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Использовать локальные данные"
        /// </summary>
        private void OnUseLocalDataClicked()
        {
            if (_rememberChoiceToggle != null && _rememberChoiceToggle.isOn)
            {
                SavePreferredStrategy(ConflictResolutionStrategy.ClientWins);
            }
            
            CompleteResolution(_localEmotionData);
        }
        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Использовать серверные данные"
        /// </summary>
        private void OnUseServerDataClicked()
        {
            if (_rememberChoiceToggle != null && _rememberChoiceToggle.isOn)
            {
                SavePreferredStrategy(ConflictResolutionStrategy.ServerWins);
            }
            
            CompleteResolution(_serverEmotionData);
        }
        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Объединить данные"
        /// </summary>
        private void OnMergeDatasClicked()
        {
            // Создаем новый объект, используя основные данные с сервера, но сохраняя локальные заметки
            EmotionData mergedData = _serverEmotionData.Clone();
            
            // Объединяем заметки, если они разные
            if (!string.IsNullOrEmpty(_localEmotionData.Note) && !string.IsNullOrEmpty(_serverEmotionData.Note)
                && _localEmotionData.Note != _serverEmotionData.Note)
            {
                mergedData.Note = $"[Локально] {_localEmotionData.Note}\n[Сервер] {_serverEmotionData.Note}";
            }
            else if (!string.IsNullOrEmpty(_localEmotionData.Note))
            {
                mergedData.Note = _localEmotionData.Note;
            }
            
            if (_rememberChoiceToggle != null && _rememberChoiceToggle.isOn)
            {
                SavePreferredStrategy(ConflictResolutionStrategy.Merge);
            }
            
            CompleteResolution(mergedData);
        }
        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Отмена"
        /// </summary>
        private void OnCancelClicked()
        {
            // По умолчанию берем серверные данные при отмене
            CompleteResolution(_serverEmotionData);
        }
        
        /// <summary>
        /// Завершает процесс разрешения конфликта, скрывая панель и устанавливая результат
        /// </summary>
        private void CompleteResolution(EmotionData resolvedData)
        {
            if (_currentResolutionTask != null && !_currentResolutionTask.Task.IsCompleted)
            {
                _currentResolutionTask.SetResult(resolvedData);
                _currentResolutionTask = null;
            }
            
            HideConflictPanel();
            _isProcessing = false;
        }
        
        /// <summary>
        /// Сохраняет предпочтительную стратегию разрешения конфликтов
        /// </summary>
        private void SavePreferredStrategy(ConflictResolutionStrategy strategy)
        {
            // Здесь можно добавить сохранение предпочтений пользователя для будущих конфликтов
            PlayerPrefs.SetInt("ConflictStrategy", (int)strategy);
            PlayerPrefs.Save();
            
            Debug.Log($"Стратегия разрешения конфликтов изменена на: {strategy}");
        }
        
        #endregion
    }
} 