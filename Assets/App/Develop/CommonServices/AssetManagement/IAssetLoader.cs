using UnityEngine;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.AssetManagement
{
    public interface IAssetLoader
    {
        Task<T> LoadAssetAsync<T>(string address) where T : Object;
        void ReleaseAsset(Object asset);
        Task<GameObject> InstantiateAsync(string address, Transform parent = null);
    }
}