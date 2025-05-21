using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Social;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = App.Develop.Utils.Logging.Logger;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Контроллер панели поиска друзей
    /// </summary>
    public class FriendSearchPanelController : MonoBehaviour, IInjectable
    {
        #region SerializeFields
        [Header("UI элементы")]
        [SerializeField] private TMP_InputField _searchInputField;
        [SerializeField] private Button _searchButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _searchResultsContainer;
        
        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private GameObject _noResultsMessage;
        
        [Header("Префабы")]
        [SerializeField] private UserSearchItemView _userSearchItemPrefab;
        #endregion
        
        #region Private Fields
        private ISocialService _socialService;
        private PanelManager _panelManager;
        private List<UserSearchItemView> _instantiatedItems = new List<UserSearchItemView>();
        private HashSet<string> _pendingRequests = new HashSet<string>();
        private bool _isInitialized = false;
        private bool _isLoading = false;
        #endregion
        
        #region Unity Lifecycle
        private void OnEnable()
        {
            ClearSearchResults();
            if (_searchInputField != null) _searchInputField.text = string.Empty;
            if (_loadingIndicator != null) _loadingIndicator.SetActive(false);
            if (_noResultsMessage != null) _noResultsMessage.SetActive(false);
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion
        
        #region Initialization
        public void Inject(DIContainer container)
        {
            try
            {
                _socialService = container.Resolve<ISocialService>();
                if (_socialService == null) Logger.LogError("❌ FriendSearchPanelController: Не удалось получить ISocialService!");
                
                _panelManager = container.Resolve<PanelManager>();
                if (_panelManager == null) Logger.LogError("❌ FriendSearchPanelController: Не удалось получить PanelManager!");
                
                SubscribeEvents();
                _isInitialized = true;
                
                Logger.Log("✅ FriendSearchPanelController успешно инициализирован");
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ Ошибка инициализации FriendSearchPanelController: {ex.Message}");
            }
        }
        
        private void SubscribeEvents()
        {
            if (_searchButton != null)
                _searchButton.onClick.AddListener(PerformSearch);
                
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
                
            if (_searchInputField != null)
                _searchInputField.onSubmit.AddListener(_ => PerformSearch());
        }
        
        private void UnsubscribeEvents()
        {
            if (_searchButton != null)
                _searchButton.onClick.RemoveListener(PerformSearch);
                
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
                
            if (_searchInputField != null)
                _searchInputField.onSubmit.RemoveAllListeners();
        }
        #endregion
        
        #region Search Functionality
        /// <summary>
        /// Выполняет поиск пользователей по введенному запросу
        /// </summary>
        /// <summary>
        /// Выполняет поиск пользователей по введенному запросу
        /// </summary>
        /// <summary>
        /// Выполняет поиск пользователей по введенному запросу
        /// </summary>
        private async void PerformSearch()
        {
            string searchQuery = _searchInputField.text.Trim();
    
            if (string.IsNullOrEmpty(searchQuery))
            {
                ShowPopup("Введите запрос для поиска");
                return;
            }
    
            if (_isLoading) return;
    
            try
            {
                _isLoading = true;
        
                // Показываем индикатор загрузки и скрываем сообщение о результатах
                if (_loadingIndicator != null) _loadingIndicator.SetActive(true);
                if (_noResultsMessage != null) _noResultsMessage.SetActive(false);
        
                // Очищаем предыдущие результаты
                ClearSearchResults();
        
                // Выполняем поиск
                var results = await _socialService.SearchUsers(searchQuery);
        
                // Проверяем, получили ли мы результаты
                if (results == null || results.Count == 0)
                {
                    if (_noResultsMessage != null) _noResultsMessage.SetActive(true);
                }
                else
                {
                    // Отображаем результаты поиска
                    foreach (var userProfile in results)
                    {
                        CreateSearchResultItem(userProfile.Value, userProfile.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ Ошибка при поиске пользователей: {ex.Message}");
                ShowPopup("Произошла ошибка при поиске. Попробуйте позже.");
            }
            finally
            {
                _isLoading = false;
                if (_loadingIndicator != null) _loadingIndicator.SetActive(false);
            }
        }
        
        /// <summary>
        /// Создает элемент результата поиска
        /// </summary>
        private void CreateSearchResultItem(UserProfile userProfile, string userId)
        {
            if (_userSearchItemPrefab == null || _searchResultsContainer == null || userProfile == null)
                return;
            
            UserSearchItemView itemInstance = Instantiate(_userSearchItemPrefab, _searchResultsContainer);
            itemInstance.Initialize(userProfile, userId, AddFriend, CancelFriendRequest);
            
            // Если запрос уже отправлен, показываем соответствующее состояние
            if (_pendingRequests.Contains(userId))
            {
                itemInstance.SetPendingState();
            }
            
            _instantiatedItems.Add(itemInstance);
        }

        
        /// <summary>
        /// Очищает список результатов поиска
        /// </summary>
        private void ClearSearchResults()
        {
            foreach (var item in _instantiatedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            
            _instantiatedItems.Clear();
        }
        #endregion
        
        #region Friend Management
        /// <summary>
        /// Отправляет запрос на добавление в друзья
        /// </summary>
        private async void AddFriend(string userId)
        {
            if (string.IsNullOrEmpty(userId) || _socialService == null)
                return;
            
            try
            {
                bool success = await _socialService.AddFriend(userId);
                
                if (success)
                {
                    ShowPopup("Запрос на добавление в друзья отправлен");
                    _pendingRequests.Add(userId);
                }
                else
                {
                    ShowPopup("Не удалось отправить запрос. Попробуйте позже.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ Ошибка при добавлении друга: {ex.Message}");
                ShowPopup("Произошла ошибка. Попробуйте позже.");
            }
        }
        
        /// <summary>
        /// Отменяет запрос на добавление в друзья
        /// </summary>
        private async void CancelFriendRequest(string userId)
        {
            if (string.IsNullOrEmpty(userId) || _socialService == null)
                return;
            
            try
            {
                bool success = await _socialService.RemoveFriend(userId);
                
                if (success)
                {
                    ShowPopup("Запрос отменен");
                    _pendingRequests.Remove(userId);
                }
                else
                {
                    ShowPopup("Не удалось отменить запрос. Попробуйте позже.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"❌ Ошибка при отмене запроса в друзья: {ex.Message}");
                ShowPopup("Произошла ошибка. Попробуйте позже.");
            }
        }
        #endregion
        
        #region UI Event Handlers
        /// <summary>
        /// Закрывает панель поиска друзей
        /// </summary>
        private void ClosePanel()
        {
            Logger.Log($"[FriendSearchPanelController] Кнопка ClosePanel нажата!");
            if (_panelManager != null)
            {
                Logger.Log($"[FriendSearchPanelController] Вызов TogglePanelAsync для {AssetAddresses.FriendSearchPanel}");
                _ = _panelManager.TogglePanelAsync<FriendSearchPanelController>(AssetAddresses.FriendSearchPanel);
            }
            else
            {
                Logger.LogError("[FriendSearchPanelController] _panelManager is null!");
            }
        }
        #endregion
        
        #region Popup Handling
        /// <summary>
        /// Показывает всплывающее сообщение
        /// </summary>
        private void ShowPopup(string message)
        {
            if (_popupPanel != null && _popupText != null)
            {
                _popupText.text = message;
                _popupPanel.SetActive(true);
                
                // Автоматически скрыть сообщение через 2 секунды
                Invoke(nameof(HidePopup), 2f);
            }
        }
        
        /// <summary>
        /// Скрывает всплывающее сообщение
        /// </summary>
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