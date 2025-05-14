using System;
using System.Collections;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.GameSystem;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Handlers;
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

                // РЕГИСТРАЦИЯ СЕРВИСОВ ПЕРЕД ИНСТАНЦИИРОВАНИЕМ UI
                Debug.Log("🔄 [PersonalAreaBootstrap] Попытка регистрации EmotionService...");
                bool emotionServiceRegistered = false;
                try
                {
                    // Получаем обязательные зависимости для EmotionService
                    var playerDataProvider = _container.Resolve<PlayerDataProvider>();
                    var configsProvider = _container.Resolve<IConfigsProvider>();
                    var emotionConfigService = _container.Resolve<EmotionConfigService>();

                    // Получаем опциональные зависимости для EmotionService
                    IPointsService pointsService = null;
                    try { pointsService = _container.Resolve<IPointsService>(); }
                    catch (InvalidOperationException) { Debug.LogWarning("[PersonalAreaBootstrap] IPointsService не зарегистрирован, EmotionService будет работать без него."); }

                    ILevelSystem levelSystem = null;
                    try { levelSystem = _container.Resolve<ILevelSystem>(); }
                    catch (InvalidOperationException) { Debug.LogWarning("[PersonalAreaBootstrap] ILevelSystem не зарегистрирован, EmotionService будет работать без него."); }

                    // Регистрируем EmotionService как синглтон с фабричным методом
                    _container.RegisterAsSingle<IEmotionService>(c => 
                        new EmotionService(playerDataProvider, configsProvider, emotionConfigService, pointsService, levelSystem)
                    );
                    Debug.Log("✅ [PersonalAreaBootstrap] EmotionService успешно зарегистрирован через RegisterAsSingle.");
                    emotionServiceRegistered = true;
                }
                catch (InvalidOperationException ioe) // Ловим ошибки резолва ОБЯЗАТЕЛЬНЫХ зависимостей
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Не удалось разрешить ОБЯЗАТЕЛЬНУЮ зависимость для EmotionService: {ioe.Message}\n{ioe.StackTrace}");
                }
                catch (Exception e) // Ловим другие возможные ошибки при регистрации
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Общая ошибка при создании или регистрации EmotionService: {e.Message}\n{e.StackTrace}");
                }

                // Если EmotionService критически важен, и не был зарегистрирован из-за отсутствия ОБЯЗАТЕЛЬНЫХ зависимостей:
                // if (!emotionServiceRegistered)
                // {
                //     Debug.LogError("❌ [PersonalAreaBootstrap] EmotionService не был зарегистрирован. Прерывание загрузки сцены.");
                //     yield break;
                // }

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

                    JarInteractionHandler jarHandler = instance.GetComponentInChildren<JarInteractionHandler>(true);
                    if (jarHandler != null)
                    {
                        // Используем _container, который был передан в метод Run
                        if (this._container != null) 
                        {
                            Debug.Log($"[PersonalAreaBootstrap] Пытаемся внедрить зависимости в JarInteractionHandler с контейнером: {this._container.GetHashCode()}");
                            jarHandler.Inject(this._container); 
                        }
                        else
                        {
                            Debug.LogError("[PersonalAreaBootstrap] DI контейнер (например, _activeSceneContainer) не доступен для JarInteractionHandler.Inject!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PersonalAreaBootstrap] JarInteractionHandler не найден на экземпляре PersonalAreaCanvas.");
                    }
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
