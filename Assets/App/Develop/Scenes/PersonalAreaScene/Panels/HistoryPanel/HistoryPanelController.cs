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
        
        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        #endregion

        #region Private Fields
        private PanelManager _panelManager;
        private EmotionService _emotionService;
        private bool _isInitialized = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            MyLogger.Log("[HistoryPanelController] OnEnable вызван - начинаем синхронизацию истории", MyLogger.LogCategory.Sync);
            
            if (_isInitialized)
            {
                LoadHistoryData();
            }
            else
            {
                MyLogger.LogWarning("[HistoryPanelController] OnEnable: панель не инициализирована");
            }
        }
        
        private void OnDisable()
        {
            MyLogger.Log("[HistoryPanelController] OnDisable вызван - сохраняем локальные изменения в облако", MyLogger.LogCategory.Sync);
            
            if (_isInitialized && _emotionService != null)
            {
                // Синхронизируем локальные изменения с облаком при закрытии панели
                SyncLocalChangesToCloud();
            }
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
        }

        private void UnsubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
        }
        #endregion

        #region UI Event Handlers
        private void LoadHistoryData()
        {
            if (!_isInitialized)
            {
                MyLogger.LogWarning("[HistoryPanelController] Попытка загрузить данные до инициализации.");
                return;
            }

            if (_emotionService == null)
            {
                MyLogger.LogError("[HistoryPanelController] EmotionService не доступен. Не могу загрузить историю.");
                return;
            }

            MyLogger.Log("[HistoryPanelController] Загрузка истории эмоций...", MyLogger.LogCategory.UI);
            
            // Запускаем синхронизацию перед загрузкой истории
            StartCoroutine(LoadHistoryWithSync());
        }

        private System.Collections.IEnumerator LoadHistoryWithSync()
        {
            // Показываем индикатор загрузки
            if (_popupPanel != null)
            {
                _popupPanel.SetActive(true);
                if (_popupText != null)
                    _popupText.text = "Загрузка истории...";
            }
            
            MyLogger.Log("[HistoryPanelController] 🔄 Начало загрузки истории с синхронизацией", MyLogger.LogCategory.Sync);
            
            // Проверяем наличие записей в истории ДО синхронизации
            int recordsBeforeSync = 0;
            if (_emotionService != null)
            {
                var historyBeforeSync = _emotionService.GetEmotionHistory().ToList();
                recordsBeforeSync = historyBeforeSync.Count;
                MyLogger.Log($"[HistoryPanelController] 📊 Записей в локальной истории ДО синхронизации: {recordsBeforeSync}", MyLogger.LogCategory.Sync);
            }
            
            // Проверяем Firebase состояние
            bool canSync = _emotionService != null && _emotionService.IsFirebaseInitialized && _emotionService.IsAuthenticated;
            MyLogger.Log($"[HistoryPanelController] 🔗 Состояние Firebase: инициализирован={_emotionService?.IsFirebaseInitialized}, аутентифицирован={_emotionService?.IsAuthenticated}", MyLogger.LogCategory.Firebase);
            
            if (canSync)
            {
                // Обновляем текст индикатора
                if (_popupText != null)
                    _popupText.text = "Получение актуальной истории...";
                
                MyLogger.Log("[HistoryPanelController] ☁️ Начинаем полную синхронизацию с Firebase (замещение локальных данных)...", MyLogger.LogCategory.Sync);
                
                // Полностью заменяем локальную историю данными из Firebase
                var refreshTask = _emotionService.ReplaceHistoryFromFirebase();
                
                // Ждем завершения задачи
                while (!refreshTask.IsCompleted)
                {
                    yield return null;
                }
                
                // Проверяем на ошибки
                if (refreshTask.IsFaulted)
                {
                    MyLogger.LogError($"[HistoryPanelController] ❌ Ошибка при синхронизации с Firebase: {refreshTask.Exception?.GetBaseException()?.Message}", MyLogger.LogCategory.Sync);
                    if (_popupText != null)
                        _popupText.text = "Ошибка синхронизации";
                    yield return new WaitForSeconds(2f);
                }
                else
                {
                    MyLogger.Log("[HistoryPanelController] ✅ Синхронизация с Firebase завершена успешно", MyLogger.LogCategory.Sync);
                }
            }
            else
            {
                MyLogger.LogWarning("[HistoryPanelController] ⚠️ Синхронизация недоступна. Используем локальные данные.", MyLogger.LogCategory.Sync);
                if (_popupText != null)
                    _popupText.text = "Загрузка локальных данных...";
                yield return new WaitForSeconds(0.5f);
            }
            
            // Проверяем наличие записей в истории ПОСЛЕ синхронизации
            int recordsAfterSync = 0;
            if (_emotionService != null)
            {
                var historyAfterSync = _emotionService.GetEmotionHistory().ToList();
                recordsAfterSync = historyAfterSync.Count;
                MyLogger.Log($"[HistoryPanelController] 📊 Записей в истории ПОСЛЕ синхронизации: {recordsAfterSync} (изменение: {recordsAfterSync - recordsBeforeSync})", MyLogger.LogCategory.Sync);
            }
            
            // Скрываем индикатор
            if (_popupPanel != null)
                _popupPanel.SetActive(false);

            // Теперь загружаем и отображаем историю
            MyLogger.Log("[HistoryPanelController] 🎨 Отображаем историю в UI...", MyLogger.LogCategory.UI);
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
            
            // СОЗДАЕМ СПИСОК КЛОНИРОВАННЫХ ЗАПИСЕЙ
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

            // НЕ очищаем снова контейнер - мы уже сделали это выше

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
        /// Синхронизирует локальные изменения с облаком при закрытии панели
        /// </summary>
        private void SyncLocalChangesToCloud()
        {
            if (_emotionService == null)
            {
                MyLogger.LogWarning("[HistoryPanelController] EmotionService недоступен для синхронизации");
                return;
            }
            
            if (!_emotionService.IsFirebaseInitialized || !_emotionService.IsAuthenticated)
            {
                MyLogger.Log("[HistoryPanelController] Firebase не инициализирован или пользователь не аутентифицирован. Пропускаем синхронизацию.", MyLogger.LogCategory.Sync);
                return;
            }
            
            try
            {
                MyLogger.Log("[HistoryPanelController] 💾 Сохраняем локальные изменения в облако...", MyLogger.LogCategory.Sync);
                
                // Запускаем синхронизацию (отправляет несинхронизированные записи в облако)
                _emotionService.StartSync();
                
                MyLogger.Log("[HistoryPanelController] ✅ Синхронизация локальных изменений запущена", MyLogger.LogCategory.Sync);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[HistoryPanelController] ❌ Ошибка при синхронизации локальных изменений: {ex.Message}", MyLogger.LogCategory.Sync);
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