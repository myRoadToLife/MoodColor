using System.Collections;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using Firebase.Auth;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            Debug.Log("Запуск Bootstrap сцены");

            var sceneSwitcher = container.Resolve<SceneSwitcher>();
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;

            if (auth.CurrentUser != null)
            {
                Debug.Log($"Пользователь уже авторизован: {auth.CurrentUser.Email}");
                sceneSwitcher.ProcessSwitchSceneFor(
                    new OutputBootstrapArgs(new PersonalAreaInputArgs()));
            }
            else
            {
                Debug.Log("Нет авторизованного пользователя. Загружаем сцену авторизации.");
                sceneSwitcher.ProcessSwitchSceneFor(
                    new OutputBootstrapArgs(new AuthSceneInputArgs()));
            }

            yield return null;
        }
    }
}
