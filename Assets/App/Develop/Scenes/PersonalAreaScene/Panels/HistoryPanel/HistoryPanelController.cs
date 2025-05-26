using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using App.Develop.CommonServices.Emotion;
using System.Collections.Generic;
using System.Linq;
using App.Develop.Scenes.PersonalAreaScene.Panels.HistoryPanel;
using System;
using System.Threading.Tasks;
using Firebase.Auth;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class HistoryPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private Transform _historyItemsContainer;
        [SerializeField] private GameObject _historyItemPrefab;

        [Header("Кнопки")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _clearHistoryButton; // Кнопка для очистки истории
        [SerializeField] private Button _syncButton; // Кнопка для принудительной синхронизации с облаком
        
        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        
        [Header("Диалоги подтверждения")]
        [SerializeField] private GameObject _confirmationDialog;
        [SerializeField] private Button _confirmDialogYesButton;
        [SerializeField] private Button _confirmDialogNoButton;
        [SerializeField] private TMP_Text _confirmDialogText;
        #endregion

        #region Private Fields
        private PanelManager _panelManager;
        private EmotionService _emotionService;
        private bool _isInitialized = false;
        private System.Action _pendingConfirmAction;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            MyLogger.Log("[HistoryPanelController] OnEnable вызван - отображаем историю", MyLogger.LogCategory.UI);
            
            if (_isInitialized)
            {
                LoadHistoryData();
            }
            else
            {
                MyLogger.LogWarning("[HistoryPanelController] OnEnable: панель не инициализирована", MyLogger.LogCategory.UI);
            }
        }
        
        private void OnDisable()
        {
            MyLogger.Log("[HistoryPanelController] OnDisable вызван - панель закрывается", MyLogger.LogCategory.UI);
            
            // Синхронизация теперь происходит автоматически в фоне, не нужно делать ничего при закрытии панели
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Initialization
        public void Inject(DIContainer container)
        {
            _panelManager = container.Resolve<PanelManager>();
            _emotionService = container.Resolve<EmotionService>();

            if (_emotionService == null)
            {
                MyLogger.LogError("[HistoryPanelController] EmotionService не удалось получить из DI контейнера!");
            }

            // Если кнопка синхронизации не назначена в инспекторе, создаем её программно
            if (_syncButton == null && _clearHistoryButton != null)
            {
                CreateSyncButton();
            }

            SubscribeEvents();
            
            _isInitialized = true;
            
            if (gameObject.activeInHierarchy)
            {
                LoadHistoryData();
            }
        }

        private void SubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
                
            if (_syncButton != null)
                _syncButton.onClick.AddListener(SyncWithCloud);
                
            if (_clearHistoryButton != null)
                _clearHistoryButton.onClick.AddListener(ShowClearHistoryConfirmation);
                
            if (_confirmDialogYesButton != null)
                _confirmDialogYesButton.onClick.AddListener(OnConfirmDialogYes);
                
            if (_confirmDialogNoButton != null)
                _confirmDialogNoButton.onClick.AddListener(OnConfirmDialogNo);
        }

        private void UnsubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
                
            if (_syncButton != null)
                _syncButton.onClick.RemoveListener(SyncWithCloud);
                
            if (_clearHistoryButton != null)
                _clearHistoryButton.onClick.RemoveListener(ShowClearHistoryConfirmation);
                
            if (_confirmDialogYesButton != null)
                _confirmDialogYesButton.onClick.RemoveListener(OnConfirmDialogYes);
                
            if (_confirmDialogNoButton != null)
                _confirmDialogNoButton.onClick.RemoveListener(OnConfirmDialogNo);
        }

        /// <summary>
        /// Создает кнопку синхронизации программно, если она не назначена в инспекторе
        /// </summary>
        private void CreateSyncButton()
        {
            try
            {
                // Находим родительский элемент кнопки очистки истории
                Transform buttonParent = _clearHistoryButton.transform.parent;
                
                // Создаем копию кнопки очистки истории
                GameObject syncButtonGO = Instantiate(_clearHistoryButton.gameObject, buttonParent);
                syncButtonGO.name = "SyncButton";
                
                // Устанавливаем позицию относительно кнопки очистки
                RectTransform syncRect = syncButtonGO.GetComponent<RectTransform>();
                RectTransform clearRect = _clearHistoryButton.GetComponent<RectTransform>();
                
                if (syncRect != null && clearRect != null)
                {
                    // Размещаем кнопку синхронизации левее кнопки очистки
                    Vector2 position = clearRect.anchoredPosition;
                    position.x -= clearRect.sizeDelta.x + 20f; // Сдвигаем влево на ширину кнопки + отступ
                    syncRect.anchoredPosition = position;
                }
                
                // Меняем текст на кнопке
                TextMeshProUGUI buttonText = syncButtonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Синхронизировать";
                }
                
                // Получаем компонент кнопки и назначаем его
                _syncButton = syncButtonGO.GetComponent<Button>();
                
                MyLogger.Log("[HistoryPanelController] Кнопка синхронизации создана программно", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[HistoryPanelController] Ошибка при создании кнопки синхронизации: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        #endregion

        #region UI Event Handlers
        private void LoadHistoryData()
        {
            if (_emotionService == null)
            {
                MyLogger.LogError("[HistoryPanelController] EmotionService не доступен для отображения истории", MyLogger.LogCategory.UI);
                return;
            }

            MyLogger.Log("[HistoryPanelController] Отображение уже загруженной истории эмоций...", MyLogger.LogCategory.UI);

            // Просто отображаем уже загруженные данные без дополнительной синхронизации
            DisplayHistory();
        }

        private void DisplayHistory()
        {
            MyLogger.Log("[HistoryPanelController] DisplayHistory вызван", MyLogger.LogCategory.UI);
            
            if (_historyItemPrefab == null)
            {
                MyLogger.LogError("[HistoryPanelController] Префаб элемента истории (_historyItemPrefab) не назначен!");
                return;
            }
            if (_historyItemsContainer == null)
            {
                 MyLogger.LogError("[HistoryPanelController] Контейнер для элементов истории (_historyItemsContainer) не назначен!");
                return;
            }

            // Очищаем старые элементы
            if (_historyItemsContainer != null)
            {
                MyLogger.Log($"[HistoryPanelController] Очищаем контейнер, количество детей: {_historyItemsContainer.childCount}", MyLogger.LogCategory.UI);
                foreach (Transform child in _historyItemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Получаем СВЕЖИЕ данные из EmotionService
            IEnumerable<EmotionHistoryEntry> historyEntries = _emotionService.GetEmotionHistory();
            
            // Проверяем что возвращается из GetEmotionHistory
            MyLogger.Log($"[HistoryPanelController] GetEmotionHistory вернул {(historyEntries == null ? "NULL" : "не NULL")}", MyLogger.LogCategory.UI);

            if (historyEntries == null)
            {
                MyLogger.LogWarning("[HistoryPanelController] GetEmotionHistory() вернул null.", MyLogger.LogCategory.UI);
                // TODO: Отобразить сообщение "Ошибка загрузки истории"
                return;
            }

            List<EmotionHistoryEntry> entriesList = historyEntries.ToList();
            
            // Проверяем количество записей в списке
            MyLogger.Log($"[HistoryPanelController] Количество записей в истории: {entriesList.Count}", MyLogger.LogCategory.UI);

            if (!entriesList.Any())
            {
                MyLogger.Log("[HistoryPanelController] История эмоций пуста (после ToList()).", MyLogger.LogCategory.UI);
                // TODO: Отобразить сообщение "История пуста" (например, активировать специальный текстовый объект)
                return;
            }
            
            // СОЗДАЕМ СПИСОК КЛОНИРОВАННЫХ ЗАПИСЕЙ - важно, чтобы избежать проблем при обновлении данных
            List<EmotionHistoryEntry> clonedEntriesList = new List<EmotionHistoryEntry>();
            foreach (var originalEntry in entriesList)
            {
                if (originalEntry != null)
                {
                    clonedEntriesList.Add(originalEntry.Clone());
                }
                else
                {
                    MyLogger.LogWarning("[HistoryPanelController] Обнаружена null запись в исходном списке истории при клонировании, пропуск.", MyLogger.LogCategory.UI);
                }
            }
            
            // Проверяем количество записей после клонирования
            MyLogger.Log($"[HistoryPanelController] Количество клонированных записей: {clonedEntriesList.Count}", MyLogger.LogCategory.UI);

            var sortedEntries = clonedEntriesList.OrderByDescending(e => e.Timestamp).ToList();
            
            // Проверяем количество записей после сортировки
            MyLogger.Log($"[HistoryPanelController] Количество отсортированных записей: {sortedEntries.Count}", MyLogger.LogCategory.UI);

            // Снова проверяем состояние контейнера для гарантии чистоты
            if (_historyItemsContainer.childCount > 0)
            {
                MyLogger.Log($"[HistoryPanelController] Повторная очистка контейнера перед созданием новых элементов, количество детей: {_historyItemsContainer.childCount}", MyLogger.LogCategory.UI);
                foreach (Transform child in _historyItemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Используем sortedEntries для отображения
            foreach (var entry in sortedEntries) 
            {
                if (entry == null) // Дополнительная проверка, хотя после клонирования и фильтрации null это маловероятно
                {
                    MyLogger.LogWarning("[HistoryPanelController] Обнаружена null запись в истории, пропуск.", MyLogger.LogCategory.UI);
                    continue;
                }
                
                // НОВЫЙ ЛОГ ЗДЕСЬ
                MyLogger.Log($"[HistoryPanelController Loop] Готовим к отображению: Timestamp='{entry.Timestamp:O}', Kind='{entry.Timestamp.Kind}', Type='{entry.EmotionData?.Type}'", MyLogger.LogCategory.UI);

                GameObject itemInstance = Instantiate(_historyItemPrefab, _historyItemsContainer);
                var itemView = itemInstance.GetComponent<HistoryItemView>(); 
                
                if (itemView != null)
                {
                    itemView.Setup(entry);
                    MyLogger.Log($"[HistoryPanelController] Элемент создан и настроен: {entry.EmotionData?.Type}", MyLogger.LogCategory.UI);
                }
                else
                {
                    MyLogger.LogError($"[HistoryPanelController] На префабе '{_historyItemPrefab.name}' отсутствует компонент HistoryItemView. Запись не будет отображена: {entry.EmotionData?.Type} @ {entry.Timestamp}", MyLogger.LogCategory.UI);
                }
            }
            
            // Финальное логирование
            MyLogger.Log($"[HistoryPanelController] DisplayHistory завершен. Создано {(_historyItemsContainer != null ? _historyItemsContainer.childCount : 0)} элементов истории.", MyLogger.LogCategory.UI);
        }
        
        private void ClosePanel()
        {
            if (_panelManager != null)
            {
                _ = _panelManager.TogglePanelAsync<HistoryPanelController>(AssetAddresses.HistoryPanel);
            }
        }
        
        /// <summary>
        /// Показывает диалог подтверждения очистки истории
        /// </summary>
        private void ShowClearHistoryConfirmation()
        {
            if (_confirmationDialog == null || _confirmDialogText == null)
            {
                // Если диалог не настроен, сразу очищаем историю
                ClearHistory();
                return;
            }
            
            _confirmDialogText.text = "Вы уверены, что хотите очистить всю историю эмоций?\nЭто действие нельзя отменить.";
            _pendingConfirmAction = ClearHistory;
            _confirmationDialog.SetActive(true);
        }
        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Да" в диалоге подтверждения
        /// </summary>
        private void OnConfirmDialogYes()
        {
            if (_confirmationDialog != null)
                _confirmationDialog.SetActive(false);
                
            _pendingConfirmAction?.Invoke();
            _pendingConfirmAction = null;
        }
        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Нет" в диалоге подтверждения
        /// </summary>
        private void OnConfirmDialogNo()
        {
            if (_confirmationDialog != null)
                _confirmationDialog.SetActive(false);
                
            _pendingConfirmAction = null;
        }
        
        /// <summary>
        /// Очищает историю эмоций
        /// </summary>
        private async void ClearHistory()
        {
            if (_emotionService == null)
            {
                MyLogger.LogError("[HistoryPanelController] EmotionService не доступен для очистки истории", MyLogger.LogCategory.ClearHistory);
                return;
            }
            
            // Показываем индикатор загрузки
            ShowPopup("Очистка истории...");
            
            MyLogger.Log("[HistoryPanelController] 🗑️ Начинаем очистку истории эмоций...", MyLogger.LogCategory.ClearHistory);
            
            // Проверяем, можно ли очистить также и облачные данные
            MyLogger.Log($"🔍 [HistoryPanelController] Проверка состояния Firebase: IsFirebaseInitialized={_emotionService.IsFirebaseInitialized}, IsAuthenticated={_emotionService.IsAuthenticated}", MyLogger.LogCategory.ClearHistory);
            bool canClearCloud = _emotionService.IsFirebaseInitialized && _emotionService.IsAuthenticated;
            MyLogger.Log($"🔍 [HistoryPanelController] Результат проверки canClearCloud: {canClearCloud}", MyLogger.LogCategory.ClearHistory);
            
            try
            {
                bool success;
                if (canClearCloud)
                {
                    MyLogger.Log("[HistoryPanelController] 🗑️ Очищаем историю локально и в облаке...", MyLogger.LogCategory.ClearHistory);
                    success = await _emotionService.ClearHistoryWithCloud();
                }
                else
                {
                    MyLogger.Log("[HistoryPanelController] 🗑️ Очищаем только локальную историю...", MyLogger.LogCategory.ClearHistory);
                    _emotionService.ClearHistory();
                    success = true;
                }
                
                if (success)
                {
                    ShowPopup("История успешно очищена");
                    MyLogger.Log("[HistoryPanelController] ✅ История эмоций успешно очищена", MyLogger.LogCategory.ClearHistory);
                }
                else
                {
                    ShowPopup("Ошибка при очистке облачных данных");
                    MyLogger.LogWarning("[HistoryPanelController] ⚠️ Ошибка при очистке облачных данных", MyLogger.LogCategory.ClearHistory);
                }
                
                // Обновляем UI
                await Task.Delay(1000);
                DisplayHistory();
            }
            catch (Exception ex)
            {
                ShowPopup("Ошибка при очистке истории");
                MyLogger.LogError($"[HistoryPanelController] ❌ Ошибка при очистке истории: {ex.Message}", MyLogger.LogCategory.ClearHistory);
            }
            
            // Скрываем индикатор
            await Task.Delay(1500);
            HidePopup();
        }

        /// <summary>
        /// Выполняет принудительную синхронизацию с облаком
        /// </summary>
        private async void SyncWithCloud()
        {
            if (_emotionService == null)
            {
                MyLogger.LogError("[HistoryPanelController] EmotionService не доступен для синхронизации с облаком", MyLogger.LogCategory.UI);
                return;
            }
            
            // Показываем индикатор загрузки
            ShowPopup("Синхронизация с облаком...");
            
            try
            {
                MyLogger.Log("[HistoryPanelController] 🔄 Начинаем принудительную синхронизацию с облаком...", MyLogger.LogCategory.UI);
                
                if (!_emotionService.IsFirebaseInitialized || !_emotionService.IsAuthenticated)
                {
                    MyLogger.LogWarning("[HistoryPanelController] Firebase не инициализирован или пользователь не авторизован", MyLogger.LogCategory.UI);
                    ShowPopup("Ошибка синхронизации: пользователь не авторизован");
                    return;
                }
                
                bool success = await _emotionService.ForceSyncWithFirebase();
                
                if (success)
                {
                    ShowPopup("Данные успешно синхронизированы");
                    MyLogger.Log("[HistoryPanelController] ✅ Данные успешно синхронизированы с облаком", MyLogger.LogCategory.UI);
                    
                    // Добавляем большую задержку для гарантии завершения всех внутренних процессов
                    await Task.Delay(1000);
                    
                    // Очищаем контейнер перед обновлением UI
                    if (_historyItemsContainer != null)
                    {
                        MyLogger.Log($"[HistoryPanelController] Принудительно очищаем контейнер перед обновлением", MyLogger.LogCategory.UI);
                        foreach (Transform child in _historyItemsContainer)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                    
                    // Делаем более явное обновление UI
                    MyLogger.Log("[HistoryPanelController] Принудительное обновление UI после синхронизации", MyLogger.LogCategory.UI);
                    
                    // Дополнительный цикл событий для обновления UI
                    await Task.Yield();
                    
                    // Повторный вызов отображения данных
                    DisplayHistory();
                }
                else
                {
                    ShowPopup("Ошибка синхронизации");
                    MyLogger.LogWarning("[HistoryPanelController] ⚠️ Ошибка при синхронизации с облаком", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                ShowPopup("Ошибка синхронизации");
                MyLogger.LogError($"[HistoryPanelController] ❌ Ошибка при синхронизации с облаком: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        #endregion

        #region Popup Handling
        private void ShowPopup(string message)
        {
            if (_popupPanel != null && _popupText != null)
            {
                _popupText.text = message;
                _popupPanel.SetActive(true);
                
                Invoke(nameof(HidePopup), 2f);
            }
        }

        private void HidePopup()
        {
            if (_popupPanel != null)
            {
                _popupPanel.SetActive(false);
            }
        }
        #endregion
    }
} 