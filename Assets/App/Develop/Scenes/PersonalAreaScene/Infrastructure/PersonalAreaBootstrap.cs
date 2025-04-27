using System;
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
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, PersonalAreaInputArgs inputArgs)
        {
            try
            {
                if (container == null)
                {
                    Debug.LogError("❌ DIContainer не может быть null");
                    yield break;
                }

                _container = container;
                
                Debug.Log("🔄 [PersonalAreaBootstrap] Инициализация контейнера...");
                _container.Initialize();
                Debug.Log("✅ [PersonalAreaBootstrap] Контейнер инициализирован");

                Debug.Log("✅ [PersonalAreaBootstrap] Сцена загружена");

                // Пытаемся резолвить загрузчик ресурсов
                Debug.Log("🔄 [PersonalAreaBootstrap] Получение ResourcesAssetLoader...");
                ResourcesAssetLoader assetLoader = null;
                try 
                {
                    assetLoader = _container.Resolve<ResourcesAssetLoader>();
                    Debug.Log("✅ [PersonalAreaBootstrap] ResourcesAssetLoader получен");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка резолва ResourcesAssetLoader: {e.Message}\n{e.StackTrace}");
                    yield break;
                }

                var factory = new MonoFactory(_container);

                Debug.Log($"🔄 [PersonalAreaBootstrap] Загрузка префаба PersonalAreaCanvas из {AssetPaths.PersonalAreaCanvas}...");
                GameObject personalAreaPrefab = null;
                try
                {
                    personalAreaPrefab = assetLoader.LoadAsset<GameObject>(AssetPaths.PersonalAreaCanvas);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка загрузки префаба: {e.Message}\n{e.StackTrace}");
                    yield break;
                }

                if (personalAreaPrefab == null)
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Не удалось загрузить префаб по пути: {AssetPaths.PersonalAreaCanvas}");
                    yield break;
                }

                Debug.Log("✅ [PersonalAreaBootstrap] Префаб PersonalAreaCanvas загружен, создаем экземпляр...");
                GameObject instance = null;
                try
                {
                    instance = Instantiate(personalAreaPrefab);
                    Debug.Log("✅ [PersonalAreaBootstrap] Экземпляр PersonalAreaCanvas создан");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка создания экземпляра префаба: {e.Message}\n{e.StackTrace}");
                    yield break;
                }

                Debug.Log("🔄 [PersonalAreaBootstrap] Инициализация компонентов...");
                try
                {
                    InitializeComponents(instance, factory);
                    Debug.Log("✅ [PersonalAreaBootstrap] Компоненты инициализированы");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка инициализации компонентов: {e.Message}\n{e.StackTrace}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [PersonalAreaBootstrap] Общая ошибка: {e.Message}\n{e.StackTrace}");
            }
        }

        private void InitializeComponents(GameObject instance, MonoFactory factory)
        {
            Debug.Log("[PersonalAreaBootstrap] Поиск компонентов в инстансе...");
            
            var uiController = instance.GetComponentInChildren<PersonalAreaUIController>();
            var manager = instance.GetComponentInChildren<PersonalAreaManager>();

            if (uiController == null)
            {
                Debug.LogError("❌ [PersonalAreaBootstrap] PersonalAreaUIController не найден на инстанцированном префабе");
                return;
            }

            if (manager == null)
            {
                Debug.LogError("❌ [PersonalAreaBootstrap] PersonalAreaManager не найден на инстанцированном префабе");
                return;
            }

            Debug.Log("[PersonalAreaBootstrap] Компоненты найдены, применяем фабрику...");
            
            try
            {
                factory.CreateOn<PersonalAreaUIController>(uiController.gameObject);
                Debug.Log("✅ [PersonalAreaBootstrap] PersonalAreaUIController создан");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка создания PersonalAreaUIController: {e.Message}\n{e.StackTrace}");
            }
            
            try
            {
                manager.Inject(_container, factory);
                Debug.Log("✅ [PersonalAreaBootstrap] PersonalAreaManager инициализирован");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка инициализации PersonalAreaManager: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
