using UnityEngine;
using TMPro;
using UnityEngine.UI;
using App.Develop.CommonServices.Firebase.Database.Models;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.Social;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Генератор элементов запросов в друзья
    /// </summary>
    public class FriendRequestsGenerator : BaseItemGenerator
    {
        [SerializeField] private FriendsPanel _friendsPanel;
        
        // Временное решение - будет заменено на получение данных из сервиса
        private Dictionary<string, SocialNotification> _pendingRequests = new Dictionary<string, SocialNotification>();
        private Dictionary<string, UserProfile> _requestProfiles = new Dictionary<string, UserProfile>();
        
        public override void Initialize(ISocialService socialService)
        {
            base.Initialize(socialService);
            
            if (_socialService != null)
            {
                // Подписываемся на событие получения уведомлений
                _socialService.OnNotificationReceived += HandleNotification;
            }
        }

        private void OnDestroy()
        {
            if (_socialService != null)
            {
                _socialService.OnNotificationReceived -= HandleNotification;
            }
        }

        private void HandleNotification(SocialNotification notification)
        {
            if (notification.Type == NotificationType.FriendRequest)
            {
                string senderId = notification.Data.ContainsKey("senderId") ? notification.Data["senderId"] : "";
                if (!string.IsNullOrEmpty(senderId) && !_pendingRequests.ContainsKey(senderId))
                {
                    _pendingRequests[senderId] = notification;
                    LoadUserProfile(senderId);
                }
            }
        }

        private async void LoadUserProfile(string userId)
        {
            // Это метод нужно будет реализовать в ISocialService
            // Временно используем SearchUsers
            var users = await _socialService.SearchUsers(userId, 1);
            if (users != null && users.ContainsKey(userId))
            {
                _requestProfiles[userId] = users[userId];
            }
        }

        /// <summary>
        /// Генерирует список запросов в друзья
        /// </summary>
        public void GenerateFriendRequests()
        {
            _friendsPanel.ShowLoading(true);
            ClearContainer();
            
            if (_pendingRequests.Count == 0)
            {
                ShowNoItemsMessage(true);
                _friendsPanel.ShowLoading(false);
                return;
            }
            
            ShowNoItemsMessage(false);
            
            foreach (var request in _pendingRequests)
            {
                var userId = request.Key;
                var requestItem = AddItem();
                
                if (_requestProfiles.ContainsKey(userId))
                {
                    ConfigureRequestItem(requestItem, _requestProfiles[userId], userId);
                }
                else
                {
                    ConfigureRequestItem(requestItem, null, userId);
                }
            }
            
            _friendsPanel.ShowLoading(false);
        }

        /// <summary>
        /// Настройка элемента запроса в друзья
        /// </summary>
        private void ConfigureRequestItem(GameObject item, UserProfile profile, string userId)
        {
            var displayNameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var avatarImage = item.transform.Find("AvatarImage")?.GetComponent<Image>();
            var acceptButton = item.transform.Find("AcceptButton")?.GetComponent<Button>();
            var declineButton = item.transform.Find("DeclineButton")?.GetComponent<Button>();

            if (displayNameText != null)
            {
                displayNameText.text = profile != null ? profile.Nickname : "Пользователь " + userId;
            }
            
            if (avatarImage != null && profile != null && !string.IsNullOrEmpty(profile.PhotoUrl))
            {
                // Загрузка аватара - можно реализовать позже
            }
            
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(() => OnAcceptFriendClicked(userId));
            }
            
            if (declineButton != null)
            {
                declineButton.onClick.AddListener(() => OnDeclineFriendClicked(userId));
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку принятия запроса в друзья
        /// </summary>
        private async void OnAcceptFriendClicked(string userId)
        {
            _friendsPanel.ShowLoading(true);
            bool success = await _socialService.AcceptFriendRequest(userId);
            if (success)
            {
                _pendingRequests.Remove(userId);
                GenerateFriendRequests();
            }
            _friendsPanel.ShowLoading(false);
        }

        /// <summary>
        /// Обработка нажатия на кнопку отклонения запроса в друзья
        /// </summary>
        private void OnDeclineFriendClicked(string userId)
        {
            _pendingRequests.Remove(userId);
            GenerateFriendRequests();
            
            // В будущем здесь может быть обращение к методу сервиса для отклонения запроса
        }
    }
}