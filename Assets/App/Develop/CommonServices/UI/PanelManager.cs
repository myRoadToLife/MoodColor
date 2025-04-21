using System;
using System.Collections.Generic;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.CommonServices.UI
{
    public class PanelManager : IDisposable
    {
        private readonly Dictionary<string, GameObject> _activePanels = new();
        private readonly ResourcesAssetLoader _assetLoader;
        private readonly MonoFactory _factory;

        public PanelManager(ResourcesAssetLoader assetLoader, MonoFactory factory)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T ShowPanel<T>(string panelPath) where T : MonoBehaviour
        {
            // Если панель уже существует, просто показываем/скрываем её
            if (_activePanels.TryGetValue(panelPath, out var existingPanel))
            {
                bool isActive = existingPanel.activeSelf;
                existingPanel.SetActive(!isActive);
                Debug.Log(isActive ? $"🔽 Панель {panelPath} скрыта" : $"🔼 Панель {panelPath} показана");
                return existingPanel.GetComponent<T>();
            }

            Debug.Log($"⚙️ Открываем панель {panelPath}");

            var prefab = _assetLoader.LoadResource<GameObject>(panelPath);
            if (prefab == null)
            {
                Debug.LogError($"❌ Префаб {panelPath} не найден в Resources");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            _activePanels[panelPath] = instance;

            var component = instance.GetComponentInChildren<T>();
            if (component == null)
            {
                Debug.LogError($"❌ Компонент {typeof(T).Name} не найден на префабе {panelPath}");
                UnityEngine.Object.Destroy(instance);
                _activePanels.Remove(panelPath);
                return null;
            }

            _factory.CreateOn<T>(component.gameObject);
            return component;
        }

        public void HidePanel(string panelPath)
        {
            if (_activePanels.TryGetValue(panelPath, out var panel))
            {
                panel.SetActive(false);
                Debug.Log($"🔽 Панель {panelPath} скрыта");
            }
        }

        public void HideAllPanels()
        {
            foreach (var panel in _activePanels.Values)
            {
                panel.SetActive(false);
            }
            Debug.Log("🔽 Все панели скрыты");
        }

        public void Dispose()
        {
            foreach (var panel in _activePanels.Values)
            {
                if (panel != null)
                {
                    UnityEngine.Object.Destroy(panel);
                }
            }
            _activePanels.Clear();
        }
    }
} 