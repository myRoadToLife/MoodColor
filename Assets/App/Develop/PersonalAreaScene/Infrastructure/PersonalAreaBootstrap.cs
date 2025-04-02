using System;
using System.Collections;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.PersonalAreaScene.Infrastructure
{
    public class PersonalAreaBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, PersonalAreaInputArgs personalAreaInputArgs)
        {
            _container = container;

            ProcessRegistration();

            yield return new WaitForSeconds(1.5f);
        }

        private void ProcessRegistration()
        {
            //Делаем регистрации для сцены главного экрана приложения
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _container.Resolve<SceneSwitcher>()
                    .ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new MainSceneInputArgs(2)));
            }
        }
    }
}
