using System;
using System.Collections.Generic;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Social;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class FriendsPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("Кнопки")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _addFriendButton;
        [SerializeField] private Button _refreshButton;
        
        [Header("Содержимое")]
        [SerializeField] private Transform _friendsListContainer;
        [SerializeField] private GameObject _emptyListMessage;
        [SerializeField] private GameObject _loadingIndicator;
        
        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMPro.TMP_Text _popupText;
        
        [Header("Префабы")]
        [SerializeField] private FriendItemView _friendItemPrefab;
        #endregion

        #region Private Fields
        private PanelManager _panelManager;
        private ISocialService _socialService;
        private List<FriendItemView> _instantiatedFriends = new List<FriendItemView>();
        private bool _isInitialized = false;
        private bool _isLoading = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (_isInitialized)
            {
                LoadFriendsData();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
            
            // Отписываемся от событий социального сервиса
            if (_socialService != null)
            {
                _socialService.OnFriendshipStatusChanged -= OnFriendshipStatusChanged;
            }
        }
        #endregion

        #region Initialization
        public void Inject(DIContainer container)
        {
            try
            {
                _panelManager = container.Resolve<PanelManager>();
                if (_panelManager == null) throw new InvalidOperationException("FriendsPanelController: PanelManager could not be resolved.");
                
                _socialService = container.Resolve<ISocialService>();
                if (_socialService == null) throw new InvalidOperationException("FriendsPanelController: ISocialService could not be resolved.");
                else
                {
                    // Подписываемся на события изменения статуса дружбы
                    _socialService.OnFriendshipStatusChanged += OnFriendshipStatusChanged;
                }

                SubscribeEvents();
                LoadFriendsData();
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error initializing FriendsPanelController: {ex.Message}", ex);
            }
        }

        private void SubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
                
            if (_addFriendButton != null)
                _addFriendButton.onClick.AddListener(ShowAddFriendPanel);
                
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshFriendsList);
        }

        private void UnsubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
                
            if (_addFriendButton != null)
                _addFriendButton.onClick.RemoveListener(ShowAddFriendPanel);
                
            if (_refreshButton != null)
                _refreshButton.onClick.RemoveListener(RefreshFriendsList);
        }
        #endregion

        #region Friends Management
        /// <summary>
        /// Загружает и отображает список друзей
        /// </summary>
        private async void LoadFriendsData()
        {
            if (_socialService == null || _isLoading)
                return;
            
            _isLoading = true;
            
            try
            {
                // Показываем индикатор загрузки
                if (_loadingIndicator != null) _loadingIndicator.SetActive(true);
                if (_emptyListMessage != null) _emptyListMessage.SetActive(false);
                
                // Очищаем текущий список
                ClearFriendsList();
                
                // Получаем список друзей
                Dictionary<string, UserProfile> friends = await _socialService.GetFriendsList();
                
                // Проверяем, получили ли мы результаты
                if (friends == null || friends.Count == 0)
                {
                    if (_emptyListMessage != null) _emptyListMessage.SetActive(true);
                }
                else
                {
                    // Отображаем список друзей
                    foreach (KeyValuePair<string, UserProfile> pair in friends)
                    {
                        string userId = pair.Key;
                        UserProfile friend = pair.Value;
                        CreateFriendItem(friend, userId);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowPopup("Произошла ошибка при загрузке списка друзей");
                throw new Exception($"Error loading friends list: {ex.Message}", ex);
            }
            finally
            {
                _isLoading = false;
                if (_loadingIndicator != null) _loadingIndicator.SetActive(false);
            }
        }
        
        /// <summary>
        /// Создает элемент друга в списке
        /// </summary>
        private void CreateFriendItem(UserProfile friendProfile, string userId)
        {
            if (_friendItemPrefab == null || _friendsListContainer == null || friendProfile == null)
                return;
            
            FriendItemView itemInstance = Instantiate(_friendItemPrefab, _friendsListContainer);
            itemInstance.Initialize(friendProfile, userId, ViewFriendProfile, RemoveFriend);
            
            _instantiatedFriends.Add(itemInstance);
        }
        
        /// <summary>
        /// Очищает список друзей
        /// </summary>
        private void ClearFriendsList()
        {
            foreach (var item in _instantiatedFriends)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            
            _instantiatedFriends.Clear();
        }
        
        /// <summary>
        /// Открывает профиль друга
        /// </summary>
        private void ViewFriendProfile(string userId)
        {
            // TODO: Реализовать открытие профиля друга
            ShowPopup($"Просмотр профиля пользователя {userId}");
        }
        
        /// <summary>
        /// Удаляет друга из списка друзей
        /// </summary>
        private async void RemoveFriend(string userId)
        {
            if (string.IsNullOrEmpty(userId) || _socialService == null)
                return;
            
            try
            {
                bool success = await _socialService.RemoveFriend(userId);
                
                if (success)
                {
                    ShowPopup("Друг удален из списка");
                    // Обновляем список друзей
                    LoadFriendsData();
                }
                else
                {
                    ShowPopup("Не удалось удалить друга. Попробуйте позже.");
                }
            }
            catch (Exception ex)
            {
                ShowPopup("Произошла ошибка. Попробуйте позже.");
                throw new Exception($"Error removing friend: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Обновляет список друзей
        /// </summary>
        private void RefreshFriendsList()
        {
            LoadFriendsData();
        }
        
        /// <summary>
        /// Обрабатывает события изменения статуса дружбы
        /// </summary>
        private void OnFriendshipStatusChanged(string userId, FriendshipStatus status)
        {
            if (status == FriendshipStatus.Friend)
            {
                // Новый друг добавлен, обновляем список
                RefreshFriendsList();
                ShowPopup("Новый друг добавлен в список");
            }
            else if (status == FriendshipStatus.None)
            {
                // Друг удален, обновляем список
                RefreshFriendsList();
            }
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// Показывает панель добавления друга
        /// </summary>
        private void ShowAddFriendPanel()
        {
            if (_panelManager != null)
            {
                _ = _panelManager.TogglePanelAsync<FriendSearchPanelController>(AssetAddresses.FriendSearchPanel);
            }
            else
            {
                throw new InvalidOperationException("[FriendsPanelController] _panelManager is null!");
            }
        }
        
        /// <summary>
        /// Закрывает панель друзей
        /// </summary>
        private void ClosePanel()
        {
            if (_panelManager != null)
            {
                _ = _panelManager.TogglePanelAsync<FriendsPanelController>(AssetAddresses.FriendsPanel);
            }
            else
            {
                throw new InvalidOperationException("[FriendsPanelController] _panelManager is null!");
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