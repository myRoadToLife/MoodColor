using App.Develop.Scenes.PersonalAreaScene.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class ProfileView : BaseUIElement, IUIComponent
    {
        #region SerializeFields
        [Header("Profile Info")]
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private Image _currentEmotionImage;
        #endregion

        #region Unity Methods
        protected override void ValidateReferences()
        {
            if (_usernameText == null) LogWarning("Текст имени пользователя не назначен в инспекторе");
            if (_currentEmotionImage == null) LogWarning("Изображение текущей эмоции не назначено в инспекторе");
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Clear();
        }

        public void Clear()
        {
            SetUsername(string.Empty);
            SetCurrentEmotion(null);
        }

        public void SetUsername(string username)
        {
            if (_usernameText == null) return;
            _usernameText.text = username;
        }

        public void SetCurrentEmotion(Sprite emotionSprite)
        {
            if (_currentEmotionImage == null) return;
            _currentEmotionImage.sprite = emotionSprite;
            _currentEmotionImage.enabled = emotionSprite != null;
        }
        #endregion
    }
} 