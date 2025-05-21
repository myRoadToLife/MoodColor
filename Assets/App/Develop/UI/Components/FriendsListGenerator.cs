using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Генератор элементов списка друзей
    /// </summary>
    public class FriendsListGenerator : BaseItemGenerator
    {
        [SerializeField] private FriendsPanel _friendsPanel;
        
        /// <summary>
        /// Генерирует список друзей на основе полученных данных
        /// </summary>
        public async void GenerateFriendsList()
        {
            _friendsPanel.ShowLoading(true);
            ClearContainer();

            var friends = await _socialService.GetFriendsList();
            
            if (friends == null || friends.Count == 0)
            {
                ShowNoItemsMessage(true);
                _friendsPanel.ShowLoading(false);
                return;
            }

            ShowNoItemsMessage(false);
            
            foreach (var friend in friends)
            {
                var friendItem = AddItem();
                ConfigureFriendItem(friendItem, friend.Value, friend.Key);
            }

            _friendsPanel.ShowLoading(false);
        }

        /// <summary>
        /// Настройка элемента списка друзей
        /// </summary>
        private void ConfigureFriendItem(GameObject item, UserProfile profile, string userId)
        {
            var displayNameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var statusText = item.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            var avatarImage = item.transform.Find("AvatarImage")?.GetComponent<Image>();
            var removeButton = item.transform.Find("RemoveButton")?.GetComponent<Button>();

            if (displayNameText != null)
                displayNameText.text = profile.Nickname;
            
            if (statusText != null)
                statusText.text = profile.IsOnline ? "Онлайн" : "Офлайн";
            
            if (avatarImage != null && !string.IsNullOrEmpty(profile.PhotoUrl))
            {
                // Загрузка аватара - можно реализовать позже
            }
            
            if (removeButton != null)
            {
                removeButton.onClick.AddListener(() => OnRemoveFriendClicked(userId));
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку удаления друга
        /// </summary>
        private async void OnRemoveFriendClicked(string userId)
        {
            _friendsPanel.ShowLoading(true);
            bool success = await _socialService.RemoveFriend(userId);
            if (success)
            {
                GenerateFriendsList();
            }
            _friendsPanel.ShowLoading(false);
        }
    }
}