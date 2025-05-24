using System;
using UnityEngine;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.Utils.Logging;

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
                if (IsShowing) MyLogger.Log("[LoadingScreen] Уже показывается.");
                else MyLogger.LogWarning("[LoadingScreen] Не инициализирован правильно для показа.");
                return;
            }

            MyLogger.Log($"[LoadingScreen] Попытка загрузки и показа UI: {_uiPrefabAddress}");
            IsShowing = true;
            gameObject.SetActive(true);

            try
            {
                _uiInstance = await _assetLoader.InstantiateAsync(_uiPrefabAddress, transform);
                if (_uiInstance != null)
                {
                    _uiInstance.SetActive(true);
                    MyLogger.Log("[LoadingScreen] UI успешно загружен и показан.");
                }
                else
                {
                    MyLogger.LogError($"[LoadingScreen] Не удалось инстанцировать UI-префаб по адресу: {_uiPrefabAddress}");
                    IsShowing = false;
                    gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[LoadingScreen] Ошибка при загрузке или показе UI: {ex.Message}\n{ex.StackTrace}");
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
            
            MyLogger.Log("[LoadingScreen] Скрытие UI...");
            IsShowing = false;
            if (_uiInstance != null)
            {
                _assetLoader.ReleaseAsset(_uiInstance);
                _uiInstance = null;
                MyLogger.Log("[LoadingScreen] UI-инстанс освобожден.");
            }
            gameObject.SetActive(false);
        }
    }
}
