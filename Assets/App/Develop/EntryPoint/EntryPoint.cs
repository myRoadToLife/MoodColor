using App.Develop.DI;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    //Тут проводим все глобальные регистрации для старта работы приложения
    public class EntryPoint : MonoBehaviour
    {

        [SerializeField] private Bootstrap _appBootstrap;
        
        private void Awake()
        {
            SetupAppSettings();
            
            DIContainer projectContainer = new DIContainer();
            //Регистрация сервисов на целый проект
            //Аналог global context из популярных DI
            //Самый родительский контейнер
            
            // _appBootstrap через сервис корутины запустим Run и передадим DI
            
        }

        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
