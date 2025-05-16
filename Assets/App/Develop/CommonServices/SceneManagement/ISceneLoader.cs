using System.Collections;
using UnityEngine.SceneManagement;

namespace App.Develop.CommonServices.SceneManagement
{
    public interface ISceneLoader
    {
        IEnumerator LoadAsync(string sceneKey, LoadSceneMode mode = LoadSceneMode.Single);
        IEnumerator UnloadCurrentAddressableScene();
    }
}
 