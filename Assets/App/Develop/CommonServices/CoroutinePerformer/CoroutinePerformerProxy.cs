using UnityEngine;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    /// <summary>
    /// Прокси-компонент для выполнения корутин
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [UsedImplicitly]
    [RequireComponent(typeof(Transform))]
    [DefaultExecutionOrder(-10000)]
    [Preserve]
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