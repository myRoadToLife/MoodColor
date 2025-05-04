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
    
            // Внедряем зависимости
            factory.CreateOn<AuthManager>(authPanelInstance);
            factory.CreateOn<ProfileSetupUI>(authPanelInstance);

            yield return null;
        }

    }
}
