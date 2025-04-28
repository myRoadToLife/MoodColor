using UnityEngine;
using JetBrains.Annotations;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    /// <summary>
    /// Фабрика для создания экземпляров CoroutinePerformer
    /// </summary>
    [PublicAPI]
    public static class CoroutinePerformerFactory
    {
        private const string ProxyGameObjectName = "[CoroutinePerformerProxy]";
        
        /// <summary>
        /// Создаёт новый экземпляр CoroutinePerformer с автоматически созданным прокси
        /// </summary>
        /// <returns>Экземпляр ICoroutinePerformer</returns>
        public static ICoroutinePerformer Create()
        {
            var proxyGameObject = new GameObject(ProxyGameObjectName);
            var proxy = proxyGameObject.AddComponent<CoroutinePerformerProxy>();
            
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(proxyGameObject);
            }
            
            return new CoroutinePerformer(proxy);
        }

        /// <summary>
        /// Создаёт новый экземпляр CoroutinePerformer с указанным прокси
        /// </summary>
        /// <param name="_proxy">MonoBehaviour, который будет использоваться как прокси для запуска корутин</param>
        /// <returns>Экземпляр ICoroutinePerformer</returns>
        public static ICoroutinePerformer Create(MonoBehaviour _proxy)
        {
            if (_proxy == null)
            {
                throw new System.ArgumentNullException(nameof(_proxy));
            }
            
            return new CoroutinePerformer(_proxy);
        }
    }
} 