using System;
using UnityEngine;
using App.Develop.CommonServices.AssetManagement;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.LoadingScreen
{
    public class LoadingScreen : MonoBehaviour, ILoadingScreen
    {
        private IAssetLoader _assetLoader;
        private string _uiPrefabAddress;
        private GameObject _uiInstance;

        public bool IsShowing { get; private set; }

        public void Initialize(IAssetLoader assetLoader, string uiPrefabAddress)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _uiPrefabAddress = uiPrefabAddress ?? throw new ArgumentNullException(nameof(uiPrefabAddress));
        }

        public async void Show()
        {
            if (IsShowing || _assetLoader == null || string.IsNullOrEmpty(_uiPrefabAddress))
            {
                if (IsShowing) Debug.Log("[LoadingScreen] Уже показывается.");
                else Debug.LogWarning("[LoadingScreen] Не инициализирован правильно для показа.");
                return;
            }

            Debug.Log($"[LoadingScreen] Попытка загрузки и показа UI: {_uiPrefabAddress}");
            IsShowing = true;
            gameObject.SetActive(true);

            try
            {
                _uiInstance = await _assetLoader.InstantiateAsync(_uiPrefabAddress, transform);
                if (_uiInstance != null)
                {
                    _uiInstance.SetActive(true);
                    Debug.Log("[LoadingScreen] UI успешно загружен и показан.");
                }
                else
                {
                    Debug.LogError($"[LoadingScreen] Не удалось инстанцировать UI-префаб по адресу: {_uiPrefabAddress}");
                    IsShowing = false;
                    gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadingScreen] Ошибка при загрузке или показе UI: {ex.Message}\n{ex.StackTrace}");
                IsShowing = false;
                gameObject.SetActive(false);
                if (_uiInstance != null)
                {
                    _assetLoader.ReleaseAsset(_uiInstance);
                    _uiInstance = null;
                }
            }
        }

        public void Hide()
        {
            if (!IsShowing && _uiInstance == null)
            {
                return;
            }
            
            Debug.Log("[LoadingScreen] Скрытие UI...");
            IsShowing = false;
            if (_uiInstance != null)
            {
                _assetLoader.ReleaseAsset(_uiInstance);
                _uiInstance = null;
                Debug.Log("[LoadingScreen] UI-инстанс освобожден.");
            }
            gameObject.SetActive(false);
        }
    }
}
