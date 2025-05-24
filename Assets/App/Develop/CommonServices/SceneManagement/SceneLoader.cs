using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.SceneManagement
{
    public class SceneLoader : ISceneLoader
    {
        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;

        public IEnumerator LoadAsync(string sceneKey, LoadSceneMode mode = LoadSceneMode.Single)
        {
            MyLogger.Log($"[SceneLoader] Попытка загрузки сцены по ключу: {sceneKey} с режимом {mode}");

            if (string.IsNullOrEmpty(sceneKey))
            {
                MyLogger.LogError("[SceneLoader] Ключ сцены не может быть пустым или null.");
                yield break;
            }

            AsyncOperationHandle<SceneInstance> loadOperation = Addressables.LoadSceneAsync(sceneKey, mode, activateOnLoad: true);

            while (!loadOperation.IsDone)
            {
                yield return null;
            }

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                MyLogger.Log($"[SceneLoader] Сцена {sceneKey} успешно загружена через Addressables.");
                if (mode == LoadSceneMode.Single)
                {
                    if (sceneKey != AssetAddresses.EmptyScene)
                    {
                        if (_currentSceneHandle.IsValid() && _currentSceneHandle.Result.Scene.IsValid())
                        {
                            MyLogger.Log($"[SceneLoader] Предыдущая сцена {_currentSceneHandle.Result.Scene.name} была активна. SceneSwitcher должен был ее выгрузить перед загрузкой {sceneKey}.");
                        }
                        _currentSceneHandle = loadOperation;
                    }
                    else if (sceneKey == AssetAddresses.EmptyScene && _currentSceneHandle.IsValid())
                    {
                         MyLogger.Log($"[SceneLoader] Загружена пустая сцена ({sceneKey}), выгружаем предыдущую Addressable сцену: {_currentSceneHandle.Result.Scene.name}");
                         var unloadOp = Addressables.UnloadSceneAsync(_currentSceneHandle, true);
                         while(!unloadOp.IsDone) yield return null;
                         if(unloadOp.Status == AsyncOperationStatus.Succeeded) {
                            MyLogger.Log($"[SceneLoader] Предыдущая Addressable сцена успешно выгружена: {_currentSceneHandle.Result.Scene.name}");
                         } else {
                            MyLogger.LogError($"[SceneLoader] Ошибка выгрузки предыдущей Addressable сцены: {_currentSceneHandle.Result.Scene.name}");
                         }
                         _currentSceneHandle = default;
                    }
                }
            }
            else
            {
                MyLogger.LogError($"[SceneLoader] Ошибка загрузки сцены {sceneKey} через Addressables: {loadOperation.OperationException}");
            }
        }
        
        public IEnumerator UnloadCurrentAddressableScene()
        {
            if (_currentSceneHandle.IsValid())
            {
                MyLogger.Log($"[SceneLoader] Явная выгрузка текущей Addressable сцены: {_currentSceneHandle.Result.Scene.name}");
                var unloadOperation = Addressables.UnloadSceneAsync(_currentSceneHandle);
                while (!unloadOperation.IsDone)
                {
                    yield return null;
                }

                if (unloadOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    MyLogger.Log($"[SceneLoader] Сцена {_currentSceneHandle.Result.Scene.name} успешно выгружена.");
                }
                else
                {
                    MyLogger.LogError($"[SceneLoader] Ошибка выгрузки сцены {_currentSceneHandle.Result.Scene.name}: {unloadOperation.OperationException}");
                }
                _currentSceneHandle = default;
            }
            else
            {
                MyLogger.LogWarning("[SceneLoader] Нет текущей Addressable сцены для выгрузки.");
            }
        }
    }
}
