using UnityEngine;
using App.Develop.CommonServices.Social;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Фабрика для создания панели управления друзьями
    /// </summary>
    [CreateAssetMenu(fileName = "FriendsPanelFactory", menuName = "MoodColor/UI/FriendsPanelFactory")]
    public class FriendsPanelFactory : ScriptableObject
    {
        [SerializeField] private GameObject _friendsPanelPrefab;
        [SerializeField] private Transform _uiRoot;
        
        private ISocialService _socialService;
        
        public void Initialize(ISocialService socialService, Transform uiRoot)
        {
            _socialService = socialService;
            _uiRoot = uiRoot;
        }
        
        /// <summary>
        /// Создает и инициализирует панель управления друзьями
        /// </summary>
        public FriendsPanel CreateFriendsPanel()
        {
            if (_friendsPanelPrefab == null)
            {
                Debug.LogError("FriendsPanelPrefab не назначен!");
                return null;
            }
            
            if (_uiRoot == null)
            {
                Debug.LogWarning("UIRoot не назначен, используется корень сцены.");
                _uiRoot = null;
            }
            
            var panelInstance = Instantiate(_friendsPanelPrefab, _uiRoot);
            var friendsPanel = panelInstance.GetComponent<FriendsPanel>();
            
            if (friendsPanel == null)
            {
                Debug.LogError("FriendsPanel компонент не найден на префабе!");
                Destroy(panelInstance);
                return null;
            }
            
            var friendsGenerator = panelInstance.GetComponentInChildren<FriendsListGenerator>();
            var requestsGenerator = panelInstance.GetComponentInChildren<FriendRequestsGenerator>();
            var searchGenerator = panelInstance.GetComponentInChildren<FriendSearchGenerator>();
            
            if (friendsGenerator == null || requestsGenerator == null || searchGenerator == null)
            {
                Debug.LogError("Не все генераторы найдены на панели друзей!");
                Destroy(panelInstance);
                return null;
            }
            
            friendsPanel.Initialize(_socialService, friendsGenerator, requestsGenerator, searchGenerator);
            friendsPanel.Hide(); // Скрываем панель до явного вызова Show()
            
            return friendsPanel;
        }
    }
}