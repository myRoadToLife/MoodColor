#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Компонент для инициализации тестовой среды Firebase
    /// </summary>
    public class FirebaseTestInitializer : MonoBehaviour
    {
        private void Awake()
        {
            // Инициализируем локатор сервисов
            FirebaseServiceLocator.Initialize();
            
            Debug.Log("Firebase test environment initialized");
        }
    }
}
#endif 