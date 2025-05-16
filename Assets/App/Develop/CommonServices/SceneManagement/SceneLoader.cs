using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using App.Develop.CommonServices.AssetManagement;

namespace App.Develop.CommonServices.SceneManagement
{
    public class SceneLoader : ISceneLoader
    {
        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;

        public IEnumerator LoadAsync(string sceneKey, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Debug.Log($"[SceneLoader] Попытка загрузки сцены по ключу: {sceneKey} с режимом {mode}");

            if (string.IsNullOrEmpty(sceneKey))
            {
                Debug.LogError("[SceneLoader] Ключ сцены не может быть пустым или null.");
                yield break;
            }

            AsyncOperationHandle<SceneInstance> loadOperation = Addressables.LoadSceneAsync(sceneKey, mode, activateOnLoad: true);

            while (!loadOperation.IsDone)
            {
                yield return null;
            }

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[SceneLoader] Сцена {sceneKey} успешно загружена через Addressables.");
                if (mode == LoadSceneMode.Single)
                {
                    if (sceneKey != AssetAddresses.EmptyScene)
                    {
                        if (_currentSceneHandle.IsValid() && _currentSceneHandle.Result.Scene.IsValid())
                        {
                            Debug.Log($"[SceneLoader] Предыдущая сцена {_currentSceneHandle.Result.Scene.name} была активна. SceneSwitcher должен был ее выгрузить перед загрузкой {sceneKey}.");
                        }
                        _currentSceneHandle = loadOperation;
                    }
                    else if (sceneKey == AssetAddresses.EmptyScene && _currentSceneHandle.IsValid())
                    {
                         Debug.Log($"[SceneLoader] Загружена пустая сцена ({sceneKey}), выгружаем предыдущую Addressable сцену: {_currentSceneHandle.Result.Scene.name}");
                         var unloadOp = Addressables.UnloadSceneAsync(_currentSceneHandle, true);
                         while(!unloadOp.IsDone) yield return null;
                         if(unloadOp.Status == AsyncOperationStatus.Succeeded) {
                            Debug.Log($"[SceneLoader] Предыдущая Addressable сцена успешно выгружена: {_currentSceneHandle.Result.Scene.name}");
                         } else {
                            Debug.LogError($"[SceneLoader] Ошибка выгрузки предыдущей Addressable сцены: {_currentSceneHandle.Result.Scene.name}");
                         }
                         _currentSceneHandle = default;
                    }
                }
            }
            else
            {
                Debug.LogError($"[SceneLoader] Ошибка загрузки сцены {sceneKey} через Addressables: {loadOperation.OperationException}");
            }
        }
        
        public IEnumerator UnloadCurrentAddressableScene()
        {
            if (_currentSceneHandle.IsValid())
            {
                Debug.Log($"[SceneLoader] Явная выгрузка текущей Addressable сцены: {_currentSceneHandle.Result.Scene.name}");
                var unloadOperation = Addressables.UnloadSceneAsync(_currentSceneHandle);
                while (!unloadOperation.IsDone)
                {
                    yield return null;
                }

                if (unloadOperation.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"[SceneLoader] Сцена {_currentSceneHandle.Result.Scene.name} успешно выгружена.");
                }
                else
                {
                    Debug.LogError($"[SceneLoader] Ошибка выгрузки сцены {_currentSceneHandle.Result.Scene.name}: {unloadOperation.OperationException}");
                }
                _currentSceneHandle = default;
            }
            else
            {
                Debug.LogWarning("[SceneLoader] Нет текущей Addressable сцены для выгрузки.");
            }
        }
    }
}
