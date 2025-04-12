using System.Collections;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.AuthScene
{
    public class AuthSceneBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, AuthSceneInputArgs authSceneInputArgs)
        {
            _container = container;

            ProcessRegistration();

            Debug.Log("Auth сцена загружена");

            var holder = new GameObject("DIContainerHolder").AddComponent<DiContainerHolder>();
            holder.SetContainer(container);


            yield return new WaitForSeconds(1f);
        }

        private void ProcessRegistration()
        {
            // Здесь можно регистрировать сервисы авторизации, если появятся
            _container.Initialize();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L)) // временно — для перехода в личный кабинет
            {
                _container.Resolve<SceneSwitcher>()
                    .ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
            }
        }
    }
}
