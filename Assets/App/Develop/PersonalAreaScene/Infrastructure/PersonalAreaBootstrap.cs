using System.Collections;
using App.Develop.AppServices.Settings;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.PersonalAreaScene.Infrastructure
{
    public class PersonalAreaBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, PersonalAreaInputArgs personalAreaInputArgs)
        {
            _container = container;
            _container.Initialize();

            Debug.Log("✅ [PersonalAreaBootstrap] сцена загружена");

            ResourcesAssetLoader assetLoader = _container.Resolve<ResourcesAssetLoader>();
            MonoFactory factory = new MonoFactory(_container);

            // Загружаем префаб SettingsPanel из ресурсов
            GameObject settingsPrefab = assetLoader.LoadResource<GameObject>("UI/SettingsPanel");

            if (settingsPrefab == null)
            {
                Debug.LogError("❌ Не найден префаб SettingsPanel в Resources/UI/SettingsPanel");
                yield break;
            }

            // Инстанцируем под канвасом
            GameObject settingsInstance = Instantiate(settingsPrefab);

            factory.CreateOn<AccountSettingsManager>(
                settingsInstance.GetComponentInChildren<AccountSettingsManager>().gameObject
            );

            yield return null;
        }
    }
}
