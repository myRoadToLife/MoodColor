using UnityEngine;

namespace App.Develop.CommonServices.AssetManagement
{
    public class ResourcesAssetLoader
    {
        public T LoadResource <T>(string path) where T : Object
            => Resources.Load<T>(path);
    }
}
