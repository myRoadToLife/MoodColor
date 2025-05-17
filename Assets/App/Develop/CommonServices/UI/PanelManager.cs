using System;
using System.Collections.Generic;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Logger = App.Develop.Utils.Logging.Logger;

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
            if (_activePanels.TryGetValue(addressableKey, out var existingPanel))
            {
                if (existingPanel == null)
                {
                    Logger.LogWarning($"🧹 Объект панели {addressableKey} уничтожен. Удаляю из словаря.");
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
                Logger.LogError($"❌ Не удалось создать экземпляр панели из Addressable: {addressableKey}");
                return null;
            }

            var component = instance.GetComponentInChildren<T>();
            if (component == null)
            {
                Logger.LogError($"❌ Компонент {typeof(T).Name} не найден в иерархии префаба {addressableKey} (поиск через GetComponentInChildren)");
                Addressables.ReleaseInstance(instance);
                return null;
            }

            _factory.InjectDependencies(component);
            
            _activePanels[addressableKey] = instance;
            instance.SetActive(true);

            return component;
        }

        public async Task<bool> TogglePanelAsync<T>(string addressableKey) where T : MonoBehaviour
        {
            Logger.Log($"[PanelManager] TogglePanelAsync вызван для ключа: {addressableKey}, тип: {typeof(T).Name}");
            if (_activePanels.TryGetValue(addressableKey, out var panel))
            {
                Logger.Log($"[PanelManager] Панель {addressableKey} найдена в _activePanels.");
                if (panel == null)
                {
                    Logger.LogWarning($"[PanelManager] 🧹 Панель {addressableKey} была уничтожена (null). Удаляем ссылку и пытаемся показать заново.");
                    _activePanels.Remove(addressableKey);
                    // Пытаемся показать заново, так как ссылка была утеряна
                    var newPanelComponent = await ShowPanelAsync<T>(addressableKey);
                    return newPanelComponent != null; // Возвращаем true, если панель успешно показана (стала активной)
                }

                bool isActive = panel.activeSelf;
                Logger.Log($"[PanelManager] Текущее состояние панели {addressableKey}: {(isActive ? "Активна" : "Неактивна")}. Меняем на: {(!isActive ? "Активна" : "Неактивна")}");
                panel.SetActive(!isActive);
                return !isActive; // Возвращаем новое состояние активности (true если стала активной, false если стала неактивной)
            }
            else
            {
                Logger.Log($"[PanelManager] Панель {addressableKey} НЕ найдена в _activePanels. Показываем новую.");
                // Панели нет в словаре, значит показываем ее
                var panelComponent = await ShowPanelAsync<T>(addressableKey);
                return panelComponent != null; // Возвращаем true, если панель успешно показана (стала активной)
            }
        }

        public void HidePanel(string panelPath)
        {
            if (_activePanels.TryGetValue(panelPath, out var panel) && panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            foreach (var panel in _activePanels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }

        public void Dispose()
        {
            foreach (var panelGo in _activePanels.Values)
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
