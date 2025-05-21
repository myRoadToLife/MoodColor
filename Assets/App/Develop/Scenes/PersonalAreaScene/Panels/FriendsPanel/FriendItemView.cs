using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Представление элемента друга в списке друзей
    /// </summary>
    public class FriendItemView : MonoBehaviour
    {
        #region SerializeFields
        [Header("UI элементы")]
        [SerializeField] private TMP_Text _userNameText;
        [SerializeField] private Button _viewProfileButton;
        [SerializeField] private Button _removeFriendButton;
        
        [Header("Опционально")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Image _onlineStatusIndicator; 
        [SerializeField] private GameObject _newActivityIndicator;
        #endregion
        
        #region Private Fields
        private string _userId;
        private Action<string> _onViewProfileClicked;
        private Action<string> _onRemoveFriendClicked;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Инициализирует элемент UI данными профиля друга
        /// </summary>
        public void Initialize(UserProfile userProfile, string userId, Action<string> onViewProfileClicked, Action<string> onRemoveFriendClicked)
        {
            if (userProfile == null)
            {
                Logger.LogError("FriendItemView: Получен null UserProfile");
                return;
            }
            
            _userId = userId;
            _userNameText.text = userProfile.Nickname ?? userProfile.Email;
            
            _onViewProfileClicked = onViewProfileClicked;
            _onRemoveFriendClicked = onRemoveFriendClicked;
            
            // Настраиваем индикатор онлайн-статуса (если есть)
            if (_onlineStatusIndicator != null)
            {
                // Получаем последнюю активность из профиля и определяем, онлайн ли пользователь
                bool isOnline = IsUserOnline(userProfile.LastActive);
                _onlineStatusIndicator.color = isOnline ? Color.green : Color.gray;
            }
            
            // Сбрасываем индикатор новой активности
            if (_newActivityIndicator != null)
            {
                _newActivityIndicator.SetActive(false);
            }
            
            // Настраиваем кнопки
            if (_viewProfileButton != null)
            {
                _viewProfileButton.onClick.RemoveAllListeners();
                _viewProfileButton.onClick.AddListener(OnViewProfileClick);
            }
            
            if (_removeFriendButton != null)
            {
                _removeFriendButton.onClick.RemoveAllListeners();
                _removeFriendButton.onClick.AddListener(OnRemoveFriendClick);
            }
        }
        
        /// <summary>
        /// Показывает индикатор новой активности
        /// </summary>
        public void ShowNewActivity(bool show)
        {
            if (_newActivityIndicator != null)
            {
                _newActivityIndicator.SetActive(show);
            }
        }
        
        /// <summary>
        /// Обновляет онлайн-статус друга
        /// </summary>
        public void UpdateOnlineStatus(bool isOnline)
        {
            if (_onlineStatusIndicator != null)
            {
                _onlineStatusIndicator.color = isOnline ? Color.green : Color.gray;
            }
        }
        #endregion
        
        #region Private Methods
        private void OnViewProfileClick()
        {
            Logger.Log($"Нажатие на кнопку просмотра профиля пользователя {_userId}");
            _onViewProfileClicked?.Invoke(_userId);
        }
        
        private void OnRemoveFriendClick()
        {
            Logger.Log($"Нажатие на кнопку удаления друга {_userId}");
            _onRemoveFriendClicked?.Invoke(_userId);
        }
        
        /// <summary>
        /// Определяет, онлайн ли пользователь на основе времени последней активности
        /// </summary>
        private bool IsUserOnline(long lastActiveTimestamp)
        {
            // Проверяем, был ли пользователь активен в последние 5 минут
            var lastActive = DateTimeOffset.FromUnixTimeMilliseconds(lastActiveTimestamp);
            var timeSinceLastActive = DateTimeOffset.UtcNow - lastActive;
            return timeSinceLastActive.TotalMinutes <= 5;
        }
        #endregion
    }
}