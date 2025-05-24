using App.Develop.CommonServices.AssetManagement;
using App.Develop.DI;
using UnityEngine;
using System.Threading.Tasks;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.UI
{
    /// <summary>
    /// Фабрика для создания UI элементов
    /// </summary>
    public class UIFactory
    {
        private readonly IAssetLoader _assetLoader;
        private readonly MonoFactory _monoFactory;

        public UIFactory(IAssetLoader assetLoader, MonoFactory monoFactory)
        {
            _assetLoader = assetLoader;
            _monoFactory = monoFactory;
        }

        /// <summary>
        /// Асинхронно создает и инстанцирует UI компонент указанного типа из Addressables.
        /// </summary>
        public async Task<T> CreateAsync<T>(string addressableKey, Transform parent = null) where T : Component
        {
            GameObject instance = await _assetLoader.InstantiateAsync(addressableKey, parent);
            if (instance == null)
            {
                MyLogger.LogError($"Failed to instantiate UI prefab from address: {addressableKey}", MyLogger.LogCategory.UI);
                return null;
            }

            T component = instance.GetComponent<T>();
            if (component == null)
            {
                MyLogger.LogError($"Instantiated UI prefab from address: {addressableKey} does not have component {typeof(T).Name}", MyLogger.LogCategory.UI);
                // Важно: Addressables.ReleaseInstance нужно вызывать для инстанцированного GameObject,
                // если компонент не найден и объект больше не нужен.
                _assetLoader.ReleaseAsset(instance); // Используем ReleaseAsset, так как AddressablesLoader обрабатывает и инстансы
                return null;
            }
            return component;
        }
    }
} 