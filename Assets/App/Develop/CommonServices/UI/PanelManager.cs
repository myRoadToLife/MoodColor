using System;
using System.Collections.Generic;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace App.Develop.CommonServices.UI
{
    public class PanelManager : IDisposable
    {
        private readonly Dictionary<string, GameObject> _activePanels = new();
        private readonly IAssetLoader _assetLoader;
        private readonly MonoFactory _factory;

        public PanelManager(IAssetLoader assetLoader, MonoFactory factory)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<T> ShowPanelAsync<T>(string addressableKey) where T : MonoBehaviour
        {
            if (_activePanels.TryGetValue(addressableKey, out GameObject existingPanel))
            {
                if (existingPanel == null)
                {
                    MyLogger.LogWarning($"🧹 Объект панели {addressableKey} уничтожен. Удаляю из словаря.");
                    _activePanels.Remove(addressableKey);
                }
                else
                {
                    existingPanel.SetActive(true);
                    return existingPanel.GetComponent<T>();
                }
            }

            GameObject instance = await _assetLoader.InstantiateAsync(addressableKey);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to instantiate panel from Addressable: {addressableKey}");
            }

            T component = instance.GetComponentInChildren<T>();
            if (component == null)
            {
                Addressables.ReleaseInstance(instance); // Release if component not found
                throw new InvalidOperationException($"Component {typeof(T).Name} not found in prefab hierarchy {addressableKey} (searched via GetComponentInChildren)");
            }

            _factory.InjectDependencies(component);
            
            _activePanels[addressableKey] = instance;
            instance.SetActive(true);

            return component;
        }

        public async Task<bool> TogglePanelAsync<T>(string addressableKey) where T : MonoBehaviour
        {
            MyLogger.Log($"[PanelManager] TogglePanelAsync вызван для ключа: {addressableKey}, тип: {typeof(T).Name}");
            if (_activePanels.TryGetValue(addressableKey, out GameObject panel))
            {
                MyLogger.Log($"[PanelManager] Панель {addressableKey} найдена в _activePanels.");
                if (panel == null)
                {
                    MyLogger.LogWarning($"[PanelManager] 🧹 Панель {addressableKey} была уничтожена (null). Удаляем ссылку и пытаемся показать заново.");
                    _activePanels.Remove(addressableKey);
                    // Пытаемся показать заново, так как ссылка была утеряна
                    T newPanelComponent = await ShowPanelAsync<T>(addressableKey);
                    return newPanelComponent != null; // Возвращаем true, если панель успешно показана (стала активной)
                }

                bool isActive = panel.activeSelf;
                MyLogger.Log($"[PanelManager] Текущее состояние панели {addressableKey}: {(isActive ? "Активна" : "Неактивна")}. Меняем на: {(!isActive ? "Активна" : "Неактивна")}");
                panel.SetActive(!isActive);
                return !isActive; // Возвращаем новое состояние активности (true если стала активной, false если стала неактивной)
            }
            else
            {
                MyLogger.Log($"[PanelManager] Панель {addressableKey} НЕ найдена в _activePanels. Показываем новую.");
                // Панели нет в словаре, значит показываем ее
                T panelComponent = await ShowPanelAsync<T>(addressableKey);
                return panelComponent != null; // Возвращаем true, если панель успешно показана (стала активной)
            }
        }

        public void HidePanel(string panelPath)
        {
            if (_activePanels.TryGetValue(panelPath, out GameObject panel) && panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            foreach (GameObject panel in _activePanels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }

        public void Dispose()
        {
            foreach (GameObject panelGo in _activePanels.Values)
            {
                if (panelGo != null)
                {
                    _assetLoader.ReleaseAsset(panelGo);
                }
            }
            _activePanels.Clear();
        }
    }
}
