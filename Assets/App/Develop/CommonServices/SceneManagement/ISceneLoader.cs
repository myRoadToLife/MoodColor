using System.Collections;
using UnityEngine.SceneManagement;

namespace App.Develop.CommonServices.SceneManagement
{
    public interface ISceneLoader
    {
        IEnumerator LoadAsync(SceneID sceneID, LoadSceneMode mode = LoadSceneMode.Single);
    }
}
 