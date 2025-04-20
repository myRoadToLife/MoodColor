using System.Collections;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.Infrastructure
{
    public class PersonalAreaBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, PersonalAreaInputArgs inputArgs)
        {
            _container = container;
            _container.Initialize();

            Debug.Log("✅ [PersonalAreaBootstrap] сцена загружена");

            var assetLoader = _container.Resolve<ResourcesAssetLoader>();
            var factory = new MonoFactory(_container);

            GameObject personalAreaPrefab = assetLoader.LoadResource<GameObject>("UI/MainCanvasArea");

            if (personalAreaPrefab == null)
            {
                Debug.LogError("❌ Не найден префаб UI/MainCanvasArea в Resources");
                yield break;
            }

            GameObject instance = Instantiate(personalAreaPrefab);

            // Получаем компоненты и делаем Inject с передачей factory
            var uiController = instance.GetComponentInChildren<PersonalAreaUIController>();
            var manager = instance.GetComponentInChildren<PersonalAreaManager>();

            if (uiController != null)
                factory.CreateOn<PersonalAreaUIController>(uiController.gameObject);

            if (manager != null)
                manager.Inject(_container, factory); // передаём factory напрямую

            yield return null;
        }
    }
}
