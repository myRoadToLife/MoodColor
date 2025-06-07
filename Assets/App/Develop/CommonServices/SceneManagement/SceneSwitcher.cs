using System;
using System.Collections;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.DI;
using App.Develop.Scenes.AuthScene;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.SceneManagement
{
    public class SceneSwitcher
    {
        private DIContainer _projectContainer;
        private readonly ICoroutinePerformer _coroutinePerformer;
        private readonly ILoadingScreen _loadingScreen;
        private readonly ISceneLoader _sceneLoader;

        private DIContainer _sceneContainer;

        public SceneSwitcher(
            ICoroutinePerformer coroutinePerformer,
            ILoadingScreen loadingScreen,
            ISceneLoader sceneLoader,
            DIContainer projectContainer)
        {
            _coroutinePerformer = coroutinePerformer;
            _loadingScreen = loadingScreen;
            _sceneLoader = sceneLoader;
            _projectContainer = projectContainer;
        }

        public void ProcessSwitchSceneFor(IOutputSceneArgs outputSceneArgs)
        {
            switch (outputSceneArgs)
            {
                case OutputBootstrapArgs bootstrapArgs:
                    _coroutinePerformer.StartCoroutine(ProcessSwitchFromBootstrapScene(bootstrapArgs));
                    break;
                case OutputPersonalAreaScreenArgs personalAreaScreenArgs:
                    _coroutinePerformer.StartCoroutine(ProcessSwitchFromPersonalAreaScene(personalAreaScreenArgs));
                    break;
                case OutputMainScreenArgs mainScreenArgs:
                    _coroutinePerformer.StartCoroutine(ProcessSwitchFromMainScreenScene(mainScreenArgs));
                    break;
                case OutputAuthSceneArgs authSceneArgs:
                    _coroutinePerformer.StartCoroutine(ProcessSwitchFromAuthScene(authSceneArgs));
                    break;
                default:
                    throw new ArgumentException(nameof(outputSceneArgs));
            }
        }

        private IEnumerator ProcessSwitchFromBootstrapScene(OutputBootstrapArgs bootstrapArgs)
        {
            switch (bootstrapArgs.NextSceneInputArgs)
            {
                case PersonalAreaInputArgs personalAreaInputArgs:
                    yield return ProcessSwitchToPersonalAreaScene(personalAreaInputArgs);
                    break;
                case AuthSceneInputArgs authSceneInputArgs:
                    yield return ProcessSwitchToAuthScene(authSceneInputArgs);
                    break;
                default:
                    throw new ArgumentException("Данный переход невозможен из Bootstrap сцены!");
            }
        }

        private IEnumerator ProcessSwitchFromAuthScene(OutputAuthSceneArgs authSceneArgs)
        {
            MyLogger.Log("🛣 [SceneSwitcher] Переход из AuthScene...", MyLogger.LogCategory.Default);

            switch (authSceneArgs.NextSceneInputArgs)
            {
                case PersonalAreaInputArgs personalAreaInputArgs:
                    MyLogger.Log("➡️ [SceneSwitcher] Переключаем сцену на PersonalArea из AuthScene", MyLogger.LogCategory.Default);
                    yield return ProcessSwitchToPersonalAreaScene(personalAreaInputArgs);
                    break;

                default:
                    MyLogger.LogError("❌ [SceneSwitcher] Неизвестный маршрут из AuthScene", MyLogger.LogCategory.Default);
                    throw new ArgumentException("Данный переход невозможен из Auth сцены!");
            }
        }

        private IEnumerator ProcessSwitchFromPersonalAreaScene(OutputPersonalAreaScreenArgs personalAreaScreenArgs)
        {
            switch (personalAreaScreenArgs.NextSceneInputArgs)
            {
                // case MainSceneInputArgs mainSceneInputArgs:
                //     yield return ProcessSwitchToMainScreenScene(mainSceneInputArgs);
                //     break;
                case AuthSceneInputArgs authSceneInputArgs:
                    yield return ProcessSwitchToAuthScene(authSceneInputArgs);
                    break;
                default:
                    throw new ArgumentException("Данный переход невозможен из PersonalArea сцены!");
            }
        }


        private IEnumerator ProcessSwitchFromMainScreenScene(OutputMainScreenArgs mainScreenArgs)
        {
            switch (mainScreenArgs.NextSceneInputArgs)
            {
                case PersonalAreaInputArgs personalAreaInputArgs:
                    yield return ProcessSwitchToPersonalAreaScene(personalAreaInputArgs);
                    break;
                default:
                    throw new ArgumentException("Данный переход невозможен из MainScreen сцены!");
            }
        }

        private IEnumerator ProcessSwitchToAuthScene(AuthSceneInputArgs inputArgs)
        {
            MyLogger.Log("🧭 [SceneSwitcher] Загружаем сцену Auth по ключу Addressable", MyLogger.LogCategory.Default);

            // Показываем загрузочный экран только если он еще не показан
            if (!_loadingScreen.IsShowing)
            {
                _loadingScreen.Show();
            }
            
            _sceneContainer?.Dispose();

            yield return _sceneLoader.LoadAsync(AssetAddresses.EmptyScene);
            yield return _sceneLoader.LoadAsync(AssetAddresses.AuthScene);

            var bootstrap = Object.FindFirstObjectByType<AuthSceneBootstrap>();

            if (bootstrap == null)
            {
                MyLogger.LogError("❌ [SceneSwitcher] AuthSceneBootstrap не найден!", MyLogger.LogCategory.Default);
                _loadingScreen.Hide();
                yield break;
            }

            _sceneContainer = new DIContainer(_projectContainer);
            yield return bootstrap.Run(_sceneContainer, inputArgs);

            // Скрываем загрузочный экран для AuthScene (это нормально, так как пользователь должен видеть форму входа)
            _loadingScreen.Hide();
        }


        private IEnumerator ProcessSwitchToPersonalAreaScene(PersonalAreaInputArgs personalAreaInputArgs)
        {
            MyLogger.Log("🧭 [SceneSwitcher] Загружаем сцену PersonalArea по ключу Addressable", MyLogger.LogCategory.Default);
            
            // Показываем загрузочный экран только если он еще не показан
            // (при автоматическом входе он уже показан из Bootstrap)
            if (!_loadingScreen.IsShowing)
            {
                _loadingScreen.Show();
            }

            _sceneContainer?.Dispose();

            yield return _sceneLoader.LoadAsync(AssetAddresses.EmptyScene);
            yield return _sceneLoader.LoadAsync(AssetAddresses.PersonalAreaScene);

            var personalAreaBootstrap = Object.FindFirstObjectByType<PersonalAreaBootstrap>();

            if (personalAreaBootstrap == null)
            {
                MyLogger.LogError("❌ [SceneSwitcher] PersonalAreaBootstrap не найден!", MyLogger.LogCategory.Default);
                _loadingScreen.Hide();
                throw new NullReferenceException(nameof(personalAreaBootstrap) + " не найден на сцене после загрузки Addressable.");
            }

            _sceneContainer = new DIContainer(_projectContainer);
            yield return personalAreaBootstrap.Run(_sceneContainer, personalAreaInputArgs);

            // НЕ скрываем загрузочный экран здесь - это теперь делает PersonalAreaBootstrap
            // для обеспечения плавного перехода при автоматическом входе
            MyLogger.Log("🎯 [SceneSwitcher] PersonalArea Bootstrap завершен, управление загрузочным экраном передано PersonalAreaBootstrap", MyLogger.LogCategory.Default);
        }

        // private IEnumerator ProcessSwitchToMainScreenScene(MainSceneInputArgs mainSceneInputArgs)
        // {
        //     _loadingScreen.Show();
        //
        //     _sceneContainer?.Dispose();
        //
        //     yield return _sceneLoader.LoadAsync(SceneID.Empty);
        //     yield return _sceneLoader.LoadAsync(SceneID.MainScreen);
        //
        //     var mainScreenBootstrap = Object.FindFirstObjectByType<MainScreenBootstrap>();
        //
        //     if (mainScreenBootstrap == null)
        //         throw new NullReferenceException(nameof(mainScreenBootstrap));
        //
        //     _sceneContainer = new DIContainer(_projectContainer);
        //     yield return mainScreenBootstrap.Run(_sceneContainer, mainSceneInputArgs);
        //
        //     _loadingScreen.Hide();
        // }
    }
}
