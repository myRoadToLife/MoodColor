using System;
using App.Develop.Services.Friends;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class UserSearchResultView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _userNameText;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Button _addFriendButton;
        
        private Action<UserModel> _onAddFriendCallback;
        private UserModel _userData;
        
        public string UserId => _userData?.Id;
        
        public void Initialize(UserModel user, Action<UserModel> onAddFriendCallback)
        {
            _userData = user;
            _onAddFriendCallback = onAddFriendCallback;
            
            // Устанавливаем данные пользователя
            if (_userNameText != null)
                _userNameText.text = user.Username;
            
            // Устанавливаем аватар, если есть
            if (_avatarImage != null && !string.IsNullOrEmpty(user.AvatarUrl))
            {
                // Здесь должна быть логика загрузки аватара
                // Например, через AssetLoader или другой сервис загрузки изображений
            }
            
            // Настраиваем кнопку
            if (_addFriendButton != null)
            {
                _addFriendButton.onClick.RemoveAllListeners();
                
                if (user.IsFriend)
                {
                    // Если пользователь уже в друзьях
                    SetButtonState(false, "Уже в друзьях");
                }
                else if (user.HasPendingRequest)
                {
                    // Если запрос уже отправлен
                    SetButtonState(false, "Запрос отправлен");
                }
                else
                {
                    // Если можно добавить в друзья
                    SetButtonState(true, "Добавить в друзья");
                    _addFriendButton.onClick.AddListener(() => _onAddFriendCallback?.Invoke(_userData));
                }
            }
        }
        
        public void SetFriendRequestSent()
        {
            SetButtonState(false, "Запрос отправлен");
        }
        
        public void SetInteractable(bool interactable)
        {
            if (_addFriendButton != null)
                _addFriendButton.interactable = interactable && _userData != null && !_userData.IsFriend && !_userData.HasPendingRequest;
        }
        
        private void SetButtonState(bool interactable, string text)
        {
            if (_addFriendButton != null)
            {
                _addFriendButton.interactable = interactable;
                
                TextMeshProUGUI buttonText = _addFriendButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = text;
            }
        }
        
        private void OnDestroy()
        {
            if (_addFriendButton != null)
                _addFriendButton.onClick.RemoveAllListeners();
            
            _onAddFriendCallback = null;
            _userData = null;
        }
    }
}