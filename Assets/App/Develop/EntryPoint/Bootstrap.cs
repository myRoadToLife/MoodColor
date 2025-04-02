using System.Collections;
using System.Collections.Generic;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    //Тут происходит инициализация начала работы
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            ILoadingScreen loadingScreen = container.Resolve<ILoadingScreen>();
            SceneSwitcher sceneSwitcher = container.Resolve<SceneSwitcher>();

            loadingScreen.Show();
            Debug.Log("Начинается инициализация сервисов");

            //Инициализация всех сервисов(данных пользователей, конфигов, инит сервисов рекламы, аналитики)

            yield return new WaitForSeconds(1.5f);

            loadingScreen.Hide();
            Debug.Log("Завершается инициализация сервисов");

            //Скрываем штору 
            //Переход на следующую сцену с помощью сервисов смены сцен
            sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new PersonalAreaInputArgs()));
        }
    }
}
