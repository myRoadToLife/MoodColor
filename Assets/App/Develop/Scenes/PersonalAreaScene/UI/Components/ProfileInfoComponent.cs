using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class ProfileInfoComponent : MonoBehaviour
    {
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private Image _currentEmotionImage;

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

        public void Clear()
        {
            SetUsername(string.Empty);
            SetCurrentEmotion(null);
        }
    }
} 