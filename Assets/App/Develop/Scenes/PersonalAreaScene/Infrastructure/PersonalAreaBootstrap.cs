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
using System.Threading.Tasks; // Для Task
// Добавляем using для AsyncOperationStatus, если он понадобится, но скорее всего нет при работе с Task<T>
// using UnityEngine.ResourceManagement.AsyncOperations;

namespace App.Develop.Scenes.PersonalAreaScene.Infrastructure
{
    public class PersonalAreaBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public async Task Run(DIContainer container, PersonalAreaInputArgs inputArgs)
        {
            try
            {
                if (container == null)
                {
                    Debug.LogError("❌ DIContainer не может быть null");
                    return;
                }

                _container = container;
                Debug.Log("✅ [PersonalAreaBootstrap] Сцена загружена");

                Debug.Log("🔄 [PersonalAreaBootstrap] Получение IAssetLoader...");
                IAssetLoader assetLoader = null;
                try 
                {
                    assetLoader = _container.Resolve<IAssetLoader>();
                    Debug.Log("✅ [PersonalAreaBootstrap] IAssetLoader получен");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка резолва IAssetLoader: {e.Message}\n{e.StackTrace}");
                    return;
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
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Ошибка при создании или регистрации EmotionService: {e.Message}\n{e.StackTrace}");
                }

                Debug.Log($"🔄 [PersonalAreaBootstrap] Загрузка префаба PersonalAreaCanvas из {AssetAddresses.PersonalAreaCanvas}...");
                GameObject personalAreaPrefab = null;
                try
                {
                    // Асинхронная загрузка префаба
                    var loadHandle = assetLoader.LoadAssetAsync<GameObject>(AssetAddresses.PersonalAreaCanvas);
                    personalAreaPrefab = await loadHandle; // Ожидаем завершения загрузки и получаем результат

                    // Проверяем, что префаб успешно загружен
                    if (personalAreaPrefab == null) // Если Task вернул null (например, ассет не найден или ошибка при загрузке)
                    {
                        Debug.LogError($"❌ [PersonalAreaBootstrap] Не удалось загрузить префаб PersonalAreaCanvas по ключу {AssetAddresses.PersonalAreaCanvas}. LoadAssetAsync вернул null.");
                        return;
                    }
                    // Если IAssetLoader.LoadAssetAsync кидает исключение при ошибке, то этот код не выполнится,
                    // и мы попадем в блок catch ниже.
                }
                catch (Exception e) // Ловим ошибки, которые мог выбросить LoadAssetAsync или await
                {
                    Debug.LogError($"❌ [PersonalAreaBootstrap] Исключение при загрузке префаба {AssetAddresses.PersonalAreaCanvas}: {e.Message}\n{e.StackTrace}");
                    return;
                }

                Debug.Log("✅ [PersonalAreaBootstrap] Префаб PersonalAreaCanvas загружен, создаем экземпляр...");
                GameObject instance = null;
                try
                {
                    instance = Instantiate(personalAreaPrefab);
                    JarInteractionHandler jarHandler = instance.GetComponentInChildren<JarInteractionHandler>(true);
                    if (jarHandler != null)
                    {
                        if (this._container != null) 
                        {
                            Debug.Log($"[PersonalAreaBootstrap] Пытаемся внедрить зависимости в JarInteractionHandler с контейнером: {this._container.GetHashCode()}");
                            jarHandler.Inject(this._container); 
                        }
                        else
                        {
                            Debug.LogError("[PersonalAreaBootstrap] DI контейнер не доступен для JarInteractionHandler.Inject!");
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
                    return;
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
                Debug.LogError($"❌ [PersonalAreaBootstrap] Общая ошибка в Run: {e.Message}\n{e.StackTrace}");
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
