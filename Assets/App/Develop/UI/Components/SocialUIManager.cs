using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.Social;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Менеджер UI социальной части приложения
    /// </summary>
    public class SocialUIManager : MonoBehaviour
    {
        [SerializeField] private Button _openFriendsPanelButton;
        [SerializeField] private FriendsPanelFactory _friendsPanelFactory;
        [SerializeField] private Transform _uiRoot;
        
        private ISocialService _socialService;
        private FriendsPanel _friendsPanel;
        
        public void Initialize(ISocialService socialService)
        {
            _socialService = socialService;
            
            if (_friendsPanelFactory != null)
            {
                _friendsPanelFactory.Initialize(_socialService, _uiRoot);
            }
            
            if (_openFriendsPanelButton != null)
            {
                _openFriendsPanelButton.onClick.AddListener(OpenFriendsPanel);
            }
        }
        
        private void OnDestroy()
        {
            if (_openFriendsPanelButton != null)
            {
                _openFriendsPanelButton.onClick.RemoveListener(OpenFriendsPanel);
            }
        }
        
        /// <summary>
        /// Открывает панель управления друзьями
        /// </summary>
        public void OpenFriendsPanel()
        {
            if (_friendsPanel == null)
            {
                if (_friendsPanelFactory == null)
                {
                    Debug.LogError("FriendsPanelFactory не назначен!");
                    return;
                }
                
                _friendsPanel = _friendsPanelFactory.CreateFriendsPanel();
            }
            
            if (_friendsPanel != null)
            {
                _friendsPanel.Show();
            }
        }
    }
}