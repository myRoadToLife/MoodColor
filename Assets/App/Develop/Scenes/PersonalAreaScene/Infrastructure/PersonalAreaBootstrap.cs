using System.Collections;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.UI;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.Infrastructure
{
    public class PersonalAreaBootstrap : MonoBehaviour
    {
        private const string MAIN_CANVAS_PATH = "UI/MainCanvasArea";
        private const string CANVAS_NOT_FOUND_ERROR = "❌ Не найден префаб UI/MainCanvasArea в Resources";

        private DIContainer _container;

        public IEnumerator Run(DIContainer container, PersonalAreaInputArgs inputArgs)
        {
            if (container == null)
            {
                Debug.LogError("❌ DIContainer не может быть null");
                yield break;
            }

            _container = container;
            _container.Initialize();

            Debug.Log("✅ [PersonalAreaBootstrap] сцена загружена");

            var assetLoader = _container.Resolve<ResourcesAssetLoader>();
            var factory = new MonoFactory(_container);

            GameObject personalAreaPrefab = assetLoader.LoadResource<GameObject>(MAIN_CANVAS_PATH);

            if (personalAreaPrefab == null)
            {
                Debug.LogError(CANVAS_NOT_FOUND_ERROR);
                yield break;
            }

            GameObject instance = Instantiate(personalAreaPrefab);
            InitializeComponents(instance, factory);
        }

        private void InitializeComponents(GameObject instance, MonoFactory factory)
        {
            var uiController = instance.GetComponentInChildren<PersonalAreaUIController>();
            var manager = instance.GetComponentInChildren<PersonalAreaManager>();

            if (uiController == null)
            {
                Debug.LogError("❌ PersonalAreaUIController не найден на инстанцированном префабе");
                return;
            }

            if (manager == null)
            {
                Debug.LogError("❌ PersonalAreaManager не найден на инстанцированном префабе");
                return;
            }

            factory.CreateOn<PersonalAreaUIController>(uiController.gameObject);
            manager.Inject(_container, factory);
        }
    }
}
