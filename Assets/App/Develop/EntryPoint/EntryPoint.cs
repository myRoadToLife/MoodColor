// Assets/App/Develop/EntryPoint/EntryPoint.cs (полный код с исправлениями)
using System;
using System.Threading.Tasks;
using App.Develop.AppServices.Auth;
using App.Develop.AppServices.Firebase.Auth;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.AppServices.Firebase.Auth.Services;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private Bootstrap _appBootstrap;
        private DIContainer _projectContainer;

        private async void Start()
        {
            try
            {
                SetupAppSettings();

                _projectContainer = new DIContainer();

                RegisterCoreServices(_projectContainer);
                
                // Инициализация Firebase должна быть первой
                if (!await InitFirebaseAsync())
                {
                    Debug.LogError("❌ Firebase не готов. Приложение не может продолжить работу.");
                    return;
                }
                
                RegisterFirebase(_projectContainer);
                RegisterAuthServices(_projectContainer);
                RegisterPersonalAreaServices(_projectContainer);

                _projectContainer.Initialize();
                
                _projectContainer.Resolve<PlayerDataProvider>().Load();

                _projectContainer.Resolve<ICoroutinePerformer>()
                    .StartPerformCoroutine(_appBootstrap.Run(_projectContainer));
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Критическая ошибка при запуске: {ex}");
            }
        }

        // Assets/App/Develop/EntryPoint/EntryPoint.cs - метод RegisterPersonalAreaServices

        private void RegisterPersonalAreaServices(DIContainer projectContainer)
        {
            try
            {
                projectContainer.RegisterAsSingle<IPersonalAreaService>(di =>
                    new PersonalAreaService(
                        di.Resolve<EmotionService>()
                    )
                ).NonLazy();
        
                Debug.Log("✅ PersonalAreaService зарегистрирован");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации PersonalAreaService: {ex.Message}");
                throw;
            }
        }

        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("✅ Настройки приложения установлены");
        }

        private async Task<bool> InitFirebaseAsync()
        {
            try
            {
                Debug.Log("🔄 Инициализация Firebase...");
                var task = FirebaseApp.CheckAndFixDependenciesAsync();
                await task;
                
                var result = task.Result;
                if (result == DependencyStatus.Available)
                {
                    Debug.Log("✅ Firebase инициализирован успешно");
                    return true;
                }
                
                Debug.LogError($"❌ Firebase не доступен: {result}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка инициализации Firebase: {ex.Message}");
                return false;
            }
        }

        private void RegisterCoreServices(DIContainer container)
        {
            try
            {
                container.RegisterAsSingle(_ => new ResourcesAssetLoader());

                container.RegisterAsSingle<ICoroutinePerformer>(di =>
                    Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer))
                );

                container.RegisterAsSingle<ILoadingScreen>(di =>
                    Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<LoadingScreen>(AssetPaths.LoadingScreen))
                );

                container.RegisterAsSingle<ISceneLoader>(_ => new SceneLoader());

                container.RegisterAsSingle(di =>
                    new SceneSwitcher(
                        di.Resolve<ICoroutinePerformer>(),
                        di.Resolve<ILoadingScreen>(),
                        di.Resolve<ISceneLoader>(),
                        di
                    )
                );

                container.RegisterAsSingle<ISaveLoadService>(_ =>
                    new SaveLoadService(new JsonSerializer(), new LocalDataRepository()));

                container.RegisterAsSingle(di =>
                    new ConfigsProviderService(di.Resolve<ResourcesAssetLoader>())
                );

                container.RegisterAsSingle(di =>
                    new PlayerDataProvider(
                        di.Resolve<ISaveLoadService>(),
                        di.Resolve<ConfigsProviderService>()
                    )
                );

                container.RegisterAsSingle(di =>
                    new EmotionService(di.Resolve<PlayerDataProvider>())
                ).NonLazy();

                container.RegisterAsSingle(di =>
                    new PanelManager(
                        di.Resolve<ResourcesAssetLoader>(),
                        new MonoFactory(di)
                    )
                ).NonLazy();
                
                Debug.Log("✅ Основные сервисы зарегистрированы");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации основных сервисов: {ex.Message}");
                throw;
            }
        }

        // Assets/App/Develop/EntryPoint/EntryPoint.cs - метод RegisterFirebase

        private void RegisterFirebase(DIContainer container)
        {
            try
            {
                // Базовые сервисы Firebase
                container.RegisterAsSingle<FirebaseAuth>(_ => FirebaseAuth.DefaultInstance).NonLazy();
        
                // Исправляем регистрацию DatabaseReference, добавляя URL базы данных
                container.RegisterAsSingle<DatabaseReference>(_ => {
                    // Замените на URL вашей Firebase базы данных
                    string databaseUrl = "https://moodcolor-3ac59-default-rtdb.firebaseio.com/";
            
                    // Получаем экземпляр базы данных с явным указанием URL
                    var database = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl);
            
                    return database.RootReference;
                }).NonLazy();

                // Основной сервис базы данных - без ID пользователя при инициализации
                container.RegisterAsSingle<DatabaseService>(di =>
                    new DatabaseService(
                        di.Resolve<DatabaseReference>()
                    )
                ).NonLazy();
        
                Debug.Log("✅ Firebase сервисы зарегистрированы");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации Firebase сервисов: {ex.Message}");
                throw;
            }
        }

        private void RegisterAuthServices(DIContainer container)
        {
            try
            {
                // Исправляем регистрацию ValidationService - добавляем фабрику
                container.RegisterAsSingle<ValidationService>(di => new ValidationService()).NonLazy();
        
                container.RegisterAsSingle<CredentialStorage>(_ => 
                    new CredentialStorage("UltraSecretKey!🔥")
                ).NonLazy();

                container.RegisterAsSingle<IAuthService>(di =>
                    new AuthService(
                        di.Resolve<FirebaseAuth>(),
                        di.Resolve<DatabaseService>(),
                        di.Resolve<ValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<UserProfileService>(di =>
                    new UserProfileService(
                        di.Resolve<DatabaseService>()
                    )
                ).NonLazy();
        
                Debug.Log("✅ Аутентификационные сервисы зарегистрированы");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации аутентификационных сервисов: {ex.Message}");
                throw;
            }
        }
    }
}