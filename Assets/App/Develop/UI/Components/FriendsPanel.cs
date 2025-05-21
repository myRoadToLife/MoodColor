using UnityEngine;
using TMPro;
using UnityEngine.UI;
using App.Develop.UI.Base;
using App.Develop.CommonServices.Social;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Панель управления друзьями
    /// </summary>
    public class FriendsPanel : BasePanel
    {
        [Header("UI Elements")]
        [SerializeField] private Button _friendsTabButton;
        [SerializeField] private Button _requestsTabButton;
        [SerializeField] private Button _searchTabButton;
        [SerializeField] private GameObject _friendsTab;
        [SerializeField] private GameObject _requestsTab;
        [SerializeField] private GameObject _searchTab;
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private Button _closeButton;

        [Header("Search UI")]
        [SerializeField] private TMP_InputField _searchInputField;
        [SerializeField] private Button _searchButton;

        private ISocialService _socialService;
        private FriendsListGenerator _friendsGenerator;
        private FriendRequestsGenerator _requestsGenerator;
        private FriendSearchGenerator _searchGenerator;

        public void Initialize(ISocialService socialService, 
                              FriendsListGenerator friendsGenerator,
                              FriendRequestsGenerator requestsGenerator,
                              FriendSearchGenerator searchGenerator)
        {
            _socialService = socialService;
            _friendsGenerator = friendsGenerator;
            _requestsGenerator = requestsGenerator;
            _searchGenerator = searchGenerator;
            
            // Инициализируем генераторы
            _friendsGenerator.Initialize(_socialService);
            _requestsGenerator.Initialize(_socialService);
            _searchGenerator.Initialize(_socialService);
            
            // Подписываемся на события кнопок
            _friendsTabButton.onClick.AddListener(ShowFriendsTab);
            _requestsTabButton.onClick.AddListener(ShowRequestsTab);
            _searchTabButton.onClick.AddListener(ShowSearchTab);
            _closeButton.onClick.AddListener(Hide);
            _searchButton.onClick.AddListener(OnSearchButtonClicked);
        }

        private void OnDestroy()
        {
            // Отписываемся от событий
            _friendsTabButton.onClick.RemoveListener(ShowFriendsTab);
            _requestsTabButton.onClick.RemoveListener(ShowRequestsTab);
            _searchTabButton.onClick.RemoveListener(ShowSearchTab);
            _closeButton.onClick.RemoveListener(Hide);
            _searchButton.onClick.RemoveListener(OnSearchButtonClicked);
        }

        public override void Show()
        {
            base.Show();
            ShowFriendsTab();
        }

        public void ShowFriendsTab()
        {
            SetActiveTab(_friendsTab);
            _friendsGenerator.GenerateFriendsList();
        }

        public void ShowRequestsTab()
        {
            SetActiveTab(_requestsTab);
            _requestsGenerator.GenerateFriendRequests();
        }

        public void ShowSearchTab()
        {
            SetActiveTab(_searchTab);
            _searchInputField.text = string.Empty;
        }

        private void OnSearchButtonClicked()
        {
            string searchQuery = _searchInputField.text;
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                _searchGenerator.SearchUsers(searchQuery);
            }
        }

        private void SetActiveTab(GameObject activeTab)
        {
            _friendsTab.SetActive(activeTab == _friendsTab);
            _requestsTab.SetActive(activeTab == _requestsTab);
            _searchTab.SetActive(activeTab == _searchTab);
        }

        public void ShowLoading(bool show)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(show);
            }
        }
    }
}