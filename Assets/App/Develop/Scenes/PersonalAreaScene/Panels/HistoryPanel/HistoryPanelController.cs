using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using App.Develop.CommonServices.Emotion;
using System.Collections.Generic;
using System.Linq;
using App.Develop.Scenes.PersonalAreaScene.Panels.HistoryPanel;

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
            if (_isInitialized)
            {
                LoadHistoryData();
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
                Debug.LogError("[HistoryPanelController] EmotionService не удалось получить из DI контейнера!");
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
                Debug.LogWarning("[HistoryPanelController] Попытка загрузить данные до инициализации.");
                return;
            }

            if (_emotionService == null)
            {
                Debug.LogError("[HistoryPanelController] EmotionService не доступен. Не могу загрузить историю.");
                return;
            }

            Debug.Log("[HistoryPanelController] Загрузка истории эмоций...");

            if (_historyItemPrefab == null)
            {
                Debug.LogError("[HistoryPanelController] Префаб элемента истории (_historyItemPrefab) не назначен!");
                return;
            }
            if (_historyItemsContainer == null)
            {
                 Debug.LogError("[HistoryPanelController] Контейнер для элементов истории (_historyItemsContainer) не назначен!");
                return;
            }

            // Очищаем старые элементы, если они есть и панель уже была инициализирована
            if (_isInitialized && _historyItemsContainer != null)
            {
                foreach (Transform child in _historyItemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            IEnumerable<EmotionHistoryEntry> historyEntries = _emotionService.GetEmotionHistory();

            if (historyEntries == null)
            {
                Debug.LogWarning("[HistoryPanelController] GetEmotionHistory() вернул null.");
                // TODO: Отобразить сообщение "Ошибка загрузки истории"
                return;
            }

            List<EmotionHistoryEntry> entriesList = historyEntries.ToList();

            if (!entriesList.Any())
            {
                Debug.Log("[HistoryPanelController] История эмоций пуста (после ToList()).");
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
                    Debug.LogWarning("[HistoryPanelController] Обнаружена null запись в исходном списке истории при клонировании, пропуск.");
                }
            }

            var sortedEntries = clonedEntriesList.OrderByDescending(e => e.Timestamp).ToList();

            // Очищаем старые элементы, если они есть и панель уже была инициализирована
            if (_isInitialized && _historyItemsContainer != null)
            {
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
                    Debug.LogWarning("[HistoryPanelController] Обнаружена null запись в истории, пропуск.");
                    continue;
                }
                
                // НОВЫЙ ЛОГ ЗДЕСЬ
                Debug.Log($"[HistoryPanelController Loop] Готовим к отображению: Timestamp='{entry.Timestamp:O}', Kind='{entry.Timestamp.Kind}', Type='{entry.EmotionData?.Type}'");

                GameObject itemInstance = Instantiate(_historyItemPrefab, _historyItemsContainer);
                var itemView = itemInstance.GetComponent<HistoryItemView>(); 
                
                if (itemView != null)
                {
                    itemView.Setup(entry);
                }
                else
                {
                    Debug.LogError($"[HistoryPanelController] На префабе '{_historyItemPrefab.name}' отсутствует компонент HistoryItemView. Запись не будет отображена: {entry.EmotionData?.Type} @ {entry.Timestamp}");
                }
            }
        }
        
        private void ClosePanel()
        {
            if (_panelManager != null)
            {
                _panelManager.TogglePanel<HistoryPanelController>(AssetPaths.PanelHistory);
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