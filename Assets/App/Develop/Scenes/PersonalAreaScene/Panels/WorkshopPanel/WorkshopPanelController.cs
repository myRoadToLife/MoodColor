using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class WorkshopPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("Кнопки")]
        [SerializeField] private Button _closeButton;
        
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
                LoadWorkshopData();
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
            LoadWorkshopData();
            
            _isInitialized = true;
        }

        private void SubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
        }

        private void UnsubscribeEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
        }
        #endregion

        #region UI Event Handlers
        private void LoadWorkshopData()
        {
            // TODO: Загрузка данных мастерской
            MyLogger.Log("Загрузка данных мастерской...");
        }
        
        private void ClosePanel()
        {
            if (_panelManager != null)
            {
                _panelManager.TogglePanelAsync<WorkshopPanelController>(AssetAddresses.WorkshopPanel);
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