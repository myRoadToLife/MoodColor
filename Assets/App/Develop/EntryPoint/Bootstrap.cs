using System.Collections;
using System.Collections.Generic;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    //Тут происходит инициализация начала работы
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            //Включаем загрузочную штору
            //Инициализация всех сервисов(данных пользователей, конфигов, инит сервисов рекламы, аналитики)
            
            yield return new WaitForSeconds(1.5f);
            
            //Скрываем штору 
            //Переход на следующую сцену с помощью сервисов смены сцен
            
        }
    }
}
