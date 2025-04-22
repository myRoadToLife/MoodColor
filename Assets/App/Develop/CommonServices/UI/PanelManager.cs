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
            if (_activePanels.TryGetValue(panelPath, out var existingPanel))
            {
                // 🔒 Защита: удалён объект, но остался в словаре
                if (existingPanel == null)
                {
                    Debug.LogWarning($"🧹 Объект панели {panelPath} уничтожен. Удаляю из словаря.");
                    _activePanels.Remove(panelPath);
                }
                else
                {
                    existingPanel.SetActive(true);
                    Debug.Log($"🔼 Панель {panelPath} уже существует, активируем.");
                    return existingPanel.GetComponent<T>();
                }
            }

            var prefab = _assetLoader.LoadResource<GameObject>(panelPath);
            if (prefab == null)
            {
                Debug.LogError($"❌ Префаб {panelPath} не найден в Resources");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            if (instance == null)
            {
                Debug.LogError($"❌ Не удалось создать экземпляр панели {panelPath}");
                return null;
            }

            var component = instance.GetComponentInChildren<T>();
            if (component == null)
            {
                Debug.LogError($"❌ Компонент {typeof(T).Name} не найден в префабе {panelPath}");
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            _activePanels[panelPath] = instance;
            _factory.CreateOn<T>(component.gameObject);

            return component;
        }

        public bool TogglePanel<T>(string panelPath) where T : MonoBehaviour
        {
            if (_activePanels.TryGetValue(panelPath, out var panel))
            {
                // Проверка на уничтоженный объект
                if (panel == null)
                {
                    Debug.LogWarning($"🧹 Панель {panelPath} была уничтожена. Удаляем ссылку.");
                    _activePanels.Remove(panelPath);
                    return ShowPanel<T>(panelPath) != null;
                }

                bool isActive = panel.activeSelf;
                panel.SetActive(!isActive);
                Debug.Log(isActive ? $"🔽 Панель {panelPath} скрыта" : $"🔼 Панель {panelPath} показана");
                return !isActive;
            }

            return ShowPanel<T>(panelPath) != null;
        }

        public void HidePanel(string panelPath)
        {
            if (_activePanels.TryGetValue(panelPath, out var panel) && panel != null)
            {
                panel.SetActive(false);
                Debug.Log($"🔽 Панель {panelPath} скрыта");
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
