using System;
using System.Collections;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
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

            _container.Initialize();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                _container.Resolve<SceneSwitcher>()
                    .ProcessSwitchSceneFor(new OutputMainScreenArgs(new PersonalAreaInputArgs()));
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                EmotionService emotion = _container.Resolve<EmotionService>();
                emotion.AddEmotion(EmotionTypes.Anger, 1);
                Debug.Log($"Я испытываю сейчас {emotion.GetEmotion(EmotionTypes.Anger).Value}");
            }


            if (Input.GetKeyDown(KeyCode.S))
            {
                _container.Resolve<PlayerDataProvider>().Save();
            }
        }
    }
}
