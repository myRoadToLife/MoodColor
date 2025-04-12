using System;
using System.Collections;
using App.Develop.AuthScene;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.DI;
using App.Develop.MainScreenScene.Infrastructure;
using App.Develop.PersonalAreaScene.Infrastructure;
using Object = UnityEngine.Object;

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
                    _coroutinePerformer.StartPerformCoroutine(ProcessSwitchFromBootstrapScene(bootstrapArgs));
                    break;
                case OutputPersonalAreaScreenArgs personalAreaScreenArgs:
                    _coroutinePerformer.StartPerformCoroutine(ProcessSwitchFromPersonalAreaScene(personalAreaScreenArgs));
                    break;
                case OutputMainScreenArgs mainScreenArgs:
                    _coroutinePerformer.StartPerformCoroutine(ProcessSwitchFromMainScreenScene(mainScreenArgs));
                    break;
                case OutputAuthSceneArgs authSceneArgs:
                    _coroutinePerformer.StartPerformCoroutine(ProcessSwitchFromAuthScene(authSceneArgs));
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
            switch (authSceneArgs.NextSceneInputArgs)
            {
                case PersonalAreaInputArgs personalAreaInputArgs:
                    yield return ProcessSwitchToPersonalAreaScene(personalAreaInputArgs);
                    break;
                default:
                    throw new ArgumentException("Данный переход невозможен из Auth сцены!");
            }
        }

        private IEnumerator ProcessSwitchFromPersonalAreaScene(OutputPersonalAreaScreenArgs personalAreaScreenArgs)
        {
            switch (personalAreaScreenArgs.NextSceneInputArgs)
            {
                case MainSceneInputArgs mainSceneInputArgs:
                    yield return ProcessSwitchToMainScreenScene(mainSceneInputArgs);
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

        private IEnumerator ProcessSwitchToAuthScene(AuthSceneInputArgs authSceneInputArgs)
        {
            _loadingScreen.Show();

            _sceneContainer?.Dispose();

            yield return _sceneLoader.LoadAsync(SceneID.Empty);
            yield return _sceneLoader.LoadAsync(SceneID.Auth);

            var authBootstrap = Object.FindFirstObjectByType<AuthSceneBootstrap>();

            if (authBootstrap == null)
                throw new NullReferenceException(nameof(authBootstrap));

            _sceneContainer = new DIContainer(_projectContainer);
            yield return authBootstrap.Run(_sceneContainer, authSceneInputArgs);

            _loadingScreen.Hide();
        }

        private IEnumerator ProcessSwitchToPersonalAreaScene(PersonalAreaInputArgs personalAreaInputArgs)
        {
            _loadingScreen.Show();

            _sceneContainer?.Dispose();

            yield return _sceneLoader.LoadAsync(SceneID.Empty);
            yield return _sceneLoader.LoadAsync(SceneID.PersonalArea);

            var personalAreaBootstrap = Object.FindFirstObjectByType<PersonalAreaBootstrap>();

            if (personalAreaBootstrap == null)
                throw new NullReferenceException(nameof(personalAreaBootstrap));

            _sceneContainer = new DIContainer(_projectContainer);
            yield return personalAreaBootstrap.Run(_sceneContainer, personalAreaInputArgs);

            _loadingScreen.Hide();
        }

        private IEnumerator ProcessSwitchToMainScreenScene(MainSceneInputArgs mainSceneInputArgs)
        {
            _loadingScreen.Show();

            _sceneContainer?.Dispose();

            yield return _sceneLoader.LoadAsync(SceneID.Empty);
            yield return _sceneLoader.LoadAsync(SceneID.MainScreen);

            var mainScreenBootstrap = Object.FindFirstObjectByType<MainScreenBootstrap>();

            if (mainScreenBootstrap == null)
                throw new NullReferenceException(nameof(mainScreenBootstrap));

            _sceneContainer = new DIContainer(_projectContainer);
            yield return mainScreenBootstrap.Run(_sceneContainer, mainSceneInputArgs);

            _loadingScreen.Hide();
        }
    }
}
