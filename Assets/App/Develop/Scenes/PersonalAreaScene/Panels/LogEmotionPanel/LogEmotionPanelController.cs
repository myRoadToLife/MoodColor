using System;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class LogEmotionPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("Кнопки")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _cancelButton;
        
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
                // Дополнительная логика при активации панели
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
            
            _isInitialized = true;
        }

        private void SubscribeEvents()
        {
            if (_saveButton != null)
                _saveButton.onClick.AddListener(async () => await SaveEmotionAsync());
            
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(async () => await CancelAndCloseAsync());
        }

        private void UnsubscribeEvents()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(async () => await SaveEmotionAsync());
            
            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(async () => await CancelAndCloseAsync());
        }
        #endregion

        #region UI Event Handlers
        private async Task SaveEmotionAsync()
        {
            // TODO: Добавить логику сохранения эмоции
            
            ShowPopup("Эмоция записана");
            await ClosePanelAsync();
        }

        private async Task CancelAndCloseAsync()
        {
            await ClosePanelAsync();
        }
        
        private async Task ClosePanelAsync()
        {
            if (_panelManager != null)
            {
                await _panelManager.TogglePanelAsync<LogEmotionPanelController>(AssetAddresses.LogEmotionPanel);
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