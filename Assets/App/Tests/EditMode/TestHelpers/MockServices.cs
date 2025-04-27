using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.DataManagement;
using UnityEngine;

namespace App.Tests.EditMode.TestHelpers
{
    public class MockSaveLoadService : ISaveLoadService
    {
        public void Save<TData>(TData data) where TData : ISaveData
        { }
        public bool TryLoad<TData>(out TData data) where TData : ISaveData
        {
            data = default;
            return false;
        }
    }

    public class MockResourcesLoader : IResourcesLoader
    {
        public T LoadAsset<T>(string path) where T : Object
        {
            if (typeof(T).IsSubclassOf(typeof(ScriptableObject)))
            {
                return ScriptableObject.CreateInstance(typeof(T)) as T;
            }
            return default;
        }

        public void UnloadAsset(Object asset) { }
    }
} 