using UnityEngine;
using JetBrains.Annotations;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    /// <summary>
    /// Прокси-компонент для выполнения корутин
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [UsedImplicitly]
    [RequireComponent(typeof(Transform))]
    public sealed class CoroutinePerformerProxy : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.DontSave;
            name = "[CoroutinePerformerProxy]";
        }

        private void OnDestroy()
        {
            // Очистка ресурсов при уничтожении объекта
            StopAllCoroutines();
        }
    }
} 