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
using App.Develop.Utils.Logging;
using UnityEngine;
using System.Threading.Tasks;
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
                    MyLogger.LogError("❌ DIContainer не может быть null", MyLogger.LogCategory.Bootstrap);
                    return;
                }

                _container = container;
                MyLogger.Log("✅ [PersonalAreaBootstrap] Сцена загружена", MyLogger.LogCategory.Bootstrap);

                MyLogger.Log("🔄 [PersonalAreaBootstrap] Получение IAssetLoader...", MyLogger.LogCategory.Bootstrap);
                IAssetLoader assetLoader = null;
                try 
                {
                    assetLoader = _container.Resolve<IAssetLoader>();
                    MyLogger.Log("✅ [PersonalAreaBootstrap] IAssetLoader получен", MyLogger.LogCategory.Bootstrap);
                }
                catch (Exception e)
                {
                    MyLogger.LogError($"❌ [PersonalAreaBootstrap] Ошибка резолва IAssetLoader: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
                    return;
                }

                var factory = new MonoFactory(_container);

                // РЕГИСТРАЦИЯ СЕРВИСОВ ПЕРЕД ИНСТАНЦИИРОВАНИЕМ UI
                MyLogger.Log("🔄 [PersonalAreaBootstrap] EmotionService должен быть уже зарегистрирован в родительском контейнере (_projectContainer).", MyLogger.LogCategory.Bootstrap);

                MyLogger.Log($"🔄 [PersonalAreaBootstrap] Загрузка префаба PersonalAreaCanvas из {AssetAddresses.PersonalAreaCanvas}...", MyLogger.LogCategory.Bootstrap);
                GameObject personalAreaPrefab = null;
                try
                {
                    // Асинхронная загрузка префаба
                    var loadHandle = assetLoader.LoadAssetAsync<GameObject>(AssetAddresses.PersonalAreaCanvas);
                    personalAreaPrefab = await loadHandle; // Ожидаем завершения загрузки и получаем результат

                    // Проверяем, что префаб успешно загружен
                    if (personalAreaPrefab == null) // Если Task вернул null (например, ассет не найден или ошибка при загрузке)
                    {
                        MyLogger.LogError($"❌ [PersonalAreaBootstrap] Не удалось загрузить префаб PersonalAreaCanvas по ключу {AssetAddresses.PersonalAreaCanvas}. LoadAssetAsync вернул null.", MyLogger.LogCategory.Bootstrap);
                        return;
                    }
                    // Если IAssetLoader.LoadAssetAsync кидает исключение при ошибке, то этот код не выполнится,
                    // и мы попадем в блок catch ниже.
                }
                catch (Exception e) // Ловим ошибки, которые мог выбросить LoadAssetAsync или await
                {
                    MyLogger.LogError($"❌ [PersonalAreaBootstrap] Исключение при загрузке префаба {AssetAddresses.PersonalAreaCanvas}: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
                    return;
                }

                MyLogger.Log("✅ [PersonalAreaBootstrap] Префаб PersonalAreaCanvas загружен, создаем экземпляр...", MyLogger.LogCategory.Bootstrap);
                GameObject instance = null;
                try
                {
                    instance = Instantiate(personalAreaPrefab);
                    
                    JarInteractionHandler jarHandler = instance.GetComponentInChildren<JarInteractionHandler>(true);
                    if (jarHandler != null)
                    {
                        if (this._container != null) 
                        {
                            MyLogger.Log($"[PersonalAreaBootstrap] Пытаемся внедрить зависимости в JarInteractionHandler с контейнером: {this._container.GetHashCode()}", MyLogger.LogCategory.Bootstrap);
                            jarHandler.Inject(this._container); 
                        }
                        else
                        {
                            MyLogger.LogError("[PersonalAreaBootstrap] DI контейнер не доступен для JarInteractionHandler.Inject!", MyLogger.LogCategory.Bootstrap);
                        }
                    }
                    else
                    {
                        MyLogger.LogWarning("[PersonalAreaBootstrap] JarInteractionHandler не найден на экземпляре PersonalAreaCanvas.", MyLogger.LogCategory.Bootstrap);
                    }
                }
                catch (Exception e)
                {
                    MyLogger.LogError($"❌ [PersonalAreaBootstrap] Ошибка создания экземпляра префаба: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
                    return;
                }

                MyLogger.Log("🔄 [PersonalAreaBootstrap] Инициализация компонентов...");
                try
                {
                    InitializeComponents(instance, factory);
                    MyLogger.Log("✅ [PersonalAreaBootstrap] Компоненты инициализированы");
                }
                catch (Exception e)
                {
                    MyLogger.LogError($"❌ [PersonalAreaBootstrap] Ошибка инициализации компонентов: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
                }
            }
            catch (Exception e)
            {
                MyLogger.LogError($"❌ [PersonalAreaBootstrap] Общая ошибка в Run: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
            }
        }

        private void InitializeComponents(GameObject instance, MonoFactory factory)
        {
            MyLogger.Log("[PersonalAreaBootstrap] Поиск компонентов в инстансе...", MyLogger.LogCategory.Bootstrap);
            
            var uiController = instance.GetComponentInChildren<PersonalAreaUIController>();
            var manager = instance.GetComponentInChildren<PersonalAreaManager>();

            if (uiController == null)
            {
                MyLogger.LogError("❌ [PersonalAreaBootstrap] PersonalAreaUIController не найден на инстанцированном префабе", MyLogger.LogCategory.Bootstrap);
                return;
            }

            if (manager == null)
            {
                MyLogger.LogError("❌ [PersonalAreaBootstrap] PersonalAreaManager не найден на инстанцированном префабе", MyLogger.LogCategory.Bootstrap);
                return;
            }

            MyLogger.Log("[PersonalAreaBootstrap] Компоненты найдены, применяем фабрику...", MyLogger.LogCategory.Bootstrap);
            
            try
            {
                factory.CreateOn<PersonalAreaUIController>(uiController.gameObject);
                MyLogger.Log("✅ [PersonalAreaBootstrap] PersonalAreaUIController создан", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception e)
            {
                MyLogger.LogError($"❌ [PersonalAreaBootstrap] Ошибка создания PersonalAreaUIController: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
            }
            
            try
            {
                manager.Inject(_container, factory);
                MyLogger.Log("✅ [PersonalAreaBootstrap] PersonalAreaManager инициализирован", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception e)
            {
                MyLogger.LogError($"❌ [PersonalAreaBootstrap] Ошибка инициализации PersonalAreaManager: {e.Message}\n{e.StackTrace}", MyLogger.LogCategory.Bootstrap);
            }
        }
    }
}
