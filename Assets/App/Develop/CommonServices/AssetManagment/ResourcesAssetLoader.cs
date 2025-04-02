using UnityEngine;

namespace App.Develop.CommonServices.AssetManagment
{
    public class ResourcesAssetLoader
    {
        public T LoadResource <T>(string path) where T : Object
            => Resources.Load<T>(path);
    }
}
