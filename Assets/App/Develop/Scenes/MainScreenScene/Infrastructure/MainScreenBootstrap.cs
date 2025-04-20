using System.Collections;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.MainScreenScene.Infrastructure
{
    public class MainScreenBootstrap : MonoBehaviour
    {
        //Это будет сцена где будут общие эмоции района или города
        private DIContainer _container;

        public IEnumerator Run(DIContainer container, MainSceneInputArgs mainSceneInputArgs)
        {
            _container = container;

            ProcessRegistration();

            Debug.Log($"Подгружаем ресурсы для уровня {mainSceneInputArgs.LevelNumber}");

            Debug.Log("Создаем персонажа или");
            Debug.Log("Сцена готова!");

            yield return new WaitForSeconds(1.5f);
        }

        private void ProcessRegistration()
        {
            //Делаем регистрации для сцены главного экрана приложения
            
            _container.Initialize();
        }

        private void Update()
        {
            
        }
    }
}
