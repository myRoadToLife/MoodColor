using System.Collections;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            var sceneSwitcher = container.Resolve<SceneSwitcher>();
            var auth = FirebaseAuth.DefaultInstance;

            Debug.Log("Запуск Bootstrap...");

            if (auth.CurrentUser != null)
            {
                Debug.Log($"Найден пользователь: {auth.CurrentUser.Email}. Проверяем валидность...");

                var reloadTask = auth.CurrentUser.ReloadAsync();

                // Ждём окончания асинхронной перезагрузки
                yield return new WaitUntil(() => reloadTask.IsCompleted);

                if (reloadTask.IsFaulted || reloadTask.IsCanceled)
                {
                    Debug.LogWarning("Сессия пользователя недействительна. Выполняем SignOut и возвращаемся в AuthScene.");
                    auth.SignOut();

                    sceneSwitcher.ProcessSwitchSceneFor(
                        new OutputBootstrapArgs(new AuthSceneInputArgs()));
                }
                else
                {
                    Debug.Log("Сессия пользователя действительна. Переход в PersonalArea.");
                    sceneSwitcher.ProcessSwitchSceneFor(
                        new OutputBootstrapArgs(new PersonalAreaInputArgs()));
                }
            }
            else
            {
                Debug.Log("Пользователь не авторизован. Переход в AuthScene.");
                sceneSwitcher.ProcessSwitchSceneFor(
                    new OutputBootstrapArgs(new AuthSceneInputArgs()));
            }
        }
    }
}
