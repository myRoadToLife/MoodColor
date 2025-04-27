using UnityEngine;

namespace App.Develop.CommonServices.AssetManagement
{
    public class ResourcesAssetLoader : IResourcesLoader
    {
        public T LoadAsset<T>(string path) where T : Object
            => Resources.Load<T>(path);

        public void UnloadAsset(Object asset)
        {
            if (asset != null)
            {
                Resources.UnloadAsset(asset);
            }
        }
    }
}
