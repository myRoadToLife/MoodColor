using System.Collections;
using App.Develop.CommonServices.Firebase.Auth;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.Scenes.AuthScene
{
    public class AuthSceneBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, AuthSceneInputArgs inputArgs)
        {
            _container = container;
            _container.Initialize();

            Debug.Log("✅ [AuthSceneBootstrap] сцена загружена");

            ResourcesAssetLoader assetLoader = _container.Resolve<ResourcesAssetLoader>();
            MonoFactory factory = new MonoFactory(_container);

            // Загружаем один AuthPanel, где уже есть оба компонента
            GameObject authPanelPrefab = assetLoader.LoadAsset<GameObject>("UI/AuthPanel");
            if (authPanelPrefab == null)
            {
                Debug.LogError("❌ Не найден префаб AuthPanel в Resources/UI/AuthPanel");
                yield break;
            }

            GameObject authPanelInstance = Instantiate(authPanelPrefab);
    
            // Получаем IAuthManager из контейнера (больше не компонент)
            var authManager = _container.Resolve<IAuthManager>();
            
            // Находим компонент AuthUIController, который уже должен быть на префабе
            var authUIController = authPanelInstance.GetComponent<AuthUIController>();
            if (authUIController == null)
            {
                Debug.LogError("❌ Не найден компонент AuthUIController на префабе AuthPanel");
            }
            else
            {
                // Инициализируем AuthUIController через IAuthManager
                authManager.Initialize(authUIController);
            }
            
            // Обрабатываем ProfileSetupUI
            var profileSetupUI = authPanelInstance.GetComponent<ProfileSetupUI>();
            if (profileSetupUI == null)
            {
                Debug.LogError("❌ Не найден компонент ProfileSetupUI на префабе AuthPanel");
            }
            else if (profileSetupUI is IInjectable injectableProfile)
            {
                injectableProfile.Inject(_container);
            }

            yield return null;
        }
    }
}
