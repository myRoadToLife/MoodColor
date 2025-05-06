using App.Develop.CommonServices.AssetManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.CommonServices.UI
{
    /// <summary>
    /// Фабрика для создания UI элементов
    /// </summary>
    public class UIFactory
    {
        private readonly ResourcesAssetLoader _assetLoader;
        private readonly MonoFactory _monoFactory;

        public UIFactory(ResourcesAssetLoader assetLoader, MonoFactory monoFactory)
        {
            _assetLoader = assetLoader;
            _monoFactory = monoFactory;
        }

        /// <summary>
        /// Создает UI компонент указанного типа
        /// </summary>
        public T Create<T>(string assetPath, Transform parent = null) where T : Component
        {
            var prefab = _assetLoader.LoadAsset<T>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load UI prefab: {assetPath}");
                return null;
            }

            return Object.Instantiate(prefab, parent);
        }
    }
} 