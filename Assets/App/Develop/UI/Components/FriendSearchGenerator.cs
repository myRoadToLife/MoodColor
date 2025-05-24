using UnityEngine;
using TMPro;
using UnityEngine.UI;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Генератор элементов результатов поиска друзей
    /// </summary>
    public class FriendSearchGenerator : BaseItemGenerator
    {
        [SerializeField] private FriendsPanel _friendsPanel;
        [SerializeField] private int _maxSearchResults = 20;
        
        /// <summary>
        /// Выполняет поиск пользователей и генерирует результаты
        /// </summary>
        public async void SearchUsers(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return;

            _friendsPanel.ShowLoading(true);
            ClearContainer();

            var searchResults = await _socialService.SearchUsers(searchQuery, _maxSearchResults);
            
            if (searchResults == null || searchResults.Count == 0)
            {
                ShowNoItemsMessage(true);
                _friendsPanel.ShowLoading(false);
                return;
            }

            ShowNoItemsMessage(false);
            
            foreach (var result in searchResults)
            {
                var searchItem = AddItem();
                ConfigureSearchResultItem(searchItem, result.Value, result.Key);
            }

            _friendsPanel.ShowLoading(false);
        }

        /// <summary>
        /// Настройка элемента результата поиска
        /// </summary>
        private void ConfigureSearchResultItem(GameObject item, UserProfile profile, string userId)
        {
            var displayNameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var avatarImage = item.transform.Find("AvatarImage")?.GetComponent<Image>();
            var addButton = item.transform.Find("AddButton")?.GetComponent<Button>();

            if (displayNameText != null)
                displayNameText.text = profile.Nickname;
            
            if (avatarImage != null && !string.IsNullOrEmpty(profile.PhotoUrl))
            {
                // Загрузка аватара - можно реализовать позже
            }
            
            if (addButton != null)
            {
                addButton.onClick.AddListener(() => OnAddFriendClicked(userId));
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку добавления друга
        /// </summary>
        private async void OnAddFriendClicked(string userId)
        {
            _friendsPanel.ShowLoading(true);
            bool success = await _socialService.AddFriend(userId);
            if (success)
            {
                // Можно добавить уведомление о том, что запрос отправлен
                MyLogger.Log($"Запрос в друзья отправлен пользователю {userId}", MyLogger.LogCategory.UI);
            }
            _friendsPanel.ShowLoading(false);
        }
    }
}