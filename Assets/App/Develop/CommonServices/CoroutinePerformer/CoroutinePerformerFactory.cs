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
        private const string PerformerGameObjectName = "[CoroutinePerformer]";
        
        /// <summary>
        /// Создаёт новый экземпляр CoroutinePerformer
        /// </summary>
        /// <returns>Экземпляр ICoroutinePerformer</returns>
        public static ICoroutinePerformer Create()
        {
            var performerGameObject = new GameObject(PerformerGameObjectName);
            var performer = performerGameObject.AddComponent<CoroutinePerformer>();
            
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(performerGameObject);
            }
            
            return performer;
        }

        /// <summary>
        /// Создаёт новый экземпляр CoroutinePerformer и присоединяет его к указанному GameObject
        /// </summary>
        /// <param name="_targetGameObject">GameObject, к которому будет присоединен CoroutinePerformer</param>
        /// <returns>Экземпляр ICoroutinePerformer</returns>
        public static ICoroutinePerformer Create(GameObject _targetGameObject)
        {
            if (_targetGameObject == null)
            {
                throw new System.ArgumentNullException(nameof(_targetGameObject));
            }
            
            return _targetGameObject.AddComponent<CoroutinePerformer>();
        }
    }
} 