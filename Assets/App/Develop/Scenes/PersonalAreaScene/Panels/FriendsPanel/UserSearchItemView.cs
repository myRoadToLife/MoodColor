using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Представление элемента результата поиска пользователя
    /// </summary>
    public class UserSearchItemView : MonoBehaviour
    {
        #region SerializeFields
        [Header("UI элементы")]
        [SerializeField] private TMP_Text _userNameText;
        [SerializeField] private Button _addFriendButton;
        [SerializeField] private Button _cancelRequestButton;
        [SerializeField] private GameObject _pendingIndicator;
        
        [Header("Опционально")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Image _userStatusIndicator;
        #endregion
        
        #region Private Fields
        private string _userId;
        private Action<string> _onAddFriendClicked;
        private Action<string> _onCancelRequestClicked;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Инициализирует элемент UI данными профиля пользователя
        /// </summary>
        public void Initialize(UserProfile userProfile, string userId, Action<string> onAddFriendClicked, Action<string> onCancelRequestClicked)
        {
            if (userProfile == null)
            {
                MyLogger.LogError("UserSearchItemView: Получен null UserProfile");
                return;
            }
            
            _userId = userId;
            _userNameText.text = userProfile.Nickname ?? userProfile.Email;
            
            _onAddFriendClicked = onAddFriendClicked;
            _onCancelRequestClicked = onCancelRequestClicked;
            
            // Настраиваем кнопки
            if (_addFriendButton != null)
            {
                _addFriendButton.onClick.RemoveAllListeners();
                _addFriendButton.onClick.AddListener(OnAddFriendClick);
            }
            
            if (_cancelRequestButton != null)
            {
                _cancelRequestButton.onClick.RemoveAllListeners();
                _cancelRequestButton.onClick.AddListener(OnCancelRequestClick);
            }
            
            // По умолчанию показываем кнопку добавления
            ShowAddButton();
        }

        /// <summary>
        /// Устанавливает состояние "запрос на добавление отправлен"
        /// </summary>
        public void SetPendingState()
        {
            if (_addFriendButton) _addFriendButton.gameObject.SetActive(false);
            if (_cancelRequestButton) _cancelRequestButton.gameObject.SetActive(true);
            if (_pendingIndicator) _pendingIndicator.SetActive(true);
        }
        
        /// <summary>
        /// Показывает кнопку добавления в друзья
        /// </summary>
        public void ShowAddButton()
        {
            if (_addFriendButton) _addFriendButton.gameObject.SetActive(true);
            if (_cancelRequestButton) _cancelRequestButton.gameObject.SetActive(false);
            if (_pendingIndicator) _pendingIndicator.SetActive(false);
        }
        #endregion
        
        #region Private Methods
        private void OnAddFriendClick()
        {
            MyLogger.Log($"Нажатие на кнопку добавления пользователя {_userId}");
            _onAddFriendClicked?.Invoke(_userId);
            SetPendingState();
        }
        
        private void OnCancelRequestClick()
        {
            MyLogger.Log($"Нажатие на кнопку отмены запроса пользователю {_userId}");
            _onCancelRequestClicked?.Invoke(_userId);
            ShowAddButton();
        }
        #endregion
    }
}