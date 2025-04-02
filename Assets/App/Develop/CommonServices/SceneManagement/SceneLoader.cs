using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Develop.CommonServices.SceneManagement
{
    public class SceneLoader : ISceneLoader
    {
        public IEnumerator LoadAsync(SceneID sceneID, LoadSceneMode mode = LoadSceneMode.Single)
        {
            AsyncOperation waitLoading = SceneManager.LoadSceneAsync(sceneID.ToString(), mode);

            while (waitLoading is { isDone: false })
                yield return null;
        }
    }
}
