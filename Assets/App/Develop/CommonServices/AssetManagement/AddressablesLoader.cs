using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.Utils.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace App.Develop.CommonServices.AssetManagement
{
    public class AddressablesLoader : IAssetLoader
    {
        private readonly Dictionary<Object, AsyncOperationHandle> _loadedAssetHandles = 
            new Dictionary<Object, AsyncOperationHandle>();
            
        private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> _instantiatedAssetHandles =
            new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

        public async Task<T> LoadAssetAsync<T>(string address) where T : Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            T result = await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded && result != null)
            {
                _loadedAssetHandles[result] = handle;
            }
            return result;
        }
        
        public async Task<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            MyLogger.Log($"[AddressablesLoader] Попытка инстанциировать: {address}", MyLogger.LogCategory.Default);
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
            GameObject instance = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && instance != null)
            {
                 _instantiatedAssetHandles[instance] = handle;
                 MyLogger.Log($"[AddressablesLoader] Успешно инстанциирован: {address}", MyLogger.LogCategory.Default);
            }
            else
            {
                MyLogger.LogError($"[AddressablesLoader] Ошибка инстанцирования: {address}, Status: {handle.Status}, Error: {handle.OperationException}", MyLogger.LogCategory.Default);
            }
            return instance;
        }

        public void ReleaseAsset(Object asset)
        {
            if (asset != null && _loadedAssetHandles.TryGetValue(asset, out var handle))
            {
                Addressables.Release(handle);
                _loadedAssetHandles.Remove(asset);
            }
            else if (asset is GameObject go)
            {
                 ReleaseInstance(go);
            }
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance != null && _instantiatedAssetHandles.TryGetValue(instance, out var handle))
            {
                Addressables.ReleaseInstance(handle);
                _instantiatedAssetHandles.Remove(instance);
            }
        }
    }
}