using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class FriendsPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("Кнопки")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _addFriendButton;
        
        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMPro.TMP_Text _popupText;
        #endregion

        #region Private Fields
        private PanelManager _panelManager;
        private bool _isInitialized = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (_isInitialized)
            {
                LoadFriendsData();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Initialization
        public void Inject(DIContainer container)
        {
            _panelManager = container.Resolve<PanelManager>();

            SubscribeEvents();
            LoadFriendsData();
            
            _isInitialized = true;
        }

        private void SubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
                
            if (_addFriendButton != null)
                _addFriendButton.onClick.AddListener(AddFriend);
        }

        private void UnsubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
                
            if (_addFriendButton != null)
                _addFriendButton.onClick.RemoveListener(AddFriend);
        }
        #endregion

        #region UI Event Handlers
        private void LoadFriendsData()
        {
            // TODO: Загрузка данных о друзьях
            Logger.Log("Загрузка списка друзей...");
        }
        
        private void AddFriend()
        {
            // TODO: Логика добавления друга
            ShowPopup("Запрос на добавление в друзья отправлен");
        }
        
        private void ClosePanel()
        {
            Logger.Log($"[FriendsPanelController] Кнопка ClosePanel нажата!");
            if (_panelManager != null)
            {
                Logger.Log($"[FriendsPanelController] Вызов TogglePanelAsync для {AssetAddresses.FriendsPanel}");
                _ = _panelManager.TogglePanelAsync<FriendsPanelController>(AssetAddresses.FriendsPanel);
            }
            else
            {
                Logger.LogError("[FriendsPanelController] _panelManager is null!");
            }
        }
        #endregion

        #region Popup Handling
        private void ShowPopup(string message)
        {
            if (_popupPanel != null && _popupText != null)
            {
                _popupText.text = message;
                _popupPanel.SetActive(true);
                
                // Автоматически скрыть сообщение через 2 секунды
                Invoke(nameof(HidePopup), 2f);
            }
        }

        private void HidePopup()
        {
            if (_popupPanel != null)
            {
                _popupPanel.SetActive(false);
            }
        }
        #endregion
    }
} 