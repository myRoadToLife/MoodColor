using System;
using System.Collections;
using UnityEngine;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    /// <summary>
    /// Интерфейс для управления корутинами
    /// </summary>
    public interface ICoroutinePerformer
    {
        /// <summary>
        /// Запускает корутину
        /// </summary>
        /// <param name="_routine">Корутина для запуска</param>
        /// <returns>Ссылка на запущенную корутину</returns>
        Coroutine StartCoroutine(IEnumerator _routine);
        
        /// <summary>
        /// Останавливает корутину
        /// </summary>
        /// <param name="_coroutine">Ссылка на корутину для остановки</param>
        void StopCoroutine(Coroutine _coroutine);
        
        /// <summary>
        /// Останавливает все запущенные корутины
        /// </summary>
        void StopAllCoroutines();
        
        /// <summary>
        /// Запускает действие с задержкой
        /// </summary>
        /// <param name="_delay">Задержка в секундах</param>
        /// <param name="_action">Действие для выполнения</param>
        /// <returns>Ссылка на запущенную корутину</returns>
        Coroutine ExecuteWithDelay(float _delay, Action _action);
        
        /// <summary>
        /// Запускает действие в следующем кадре
        /// </summary>
        /// <param name="_action">Действие для выполнения</param>
        /// <returns>Ссылка на запущенную корутину</returns>
        Coroutine ExecuteNextFrame(Action _action);
        
        /// <summary>
        /// Запускает действие, которое выполняется пока условие истинно
        /// </summary>
        /// <param name="_condition">Условие выполнения</param>
        /// <param name="_action">Действие для выполнения</param>
        /// <returns>Ссылка на запущенную корутину</returns>
        Coroutine ExecuteWhile(Func<bool> _condition, Action _action);
        
        /// <summary>
        /// Запускает действие, которое выполняется до тех пор, пока условие не станет истинным
        /// </summary>
        /// <param name="_predicate">Условие для проверки</param>
        /// <param name="_onComplete">Действие, выполняемое при успешном завершении</param>
        /// <param name="_timeout">Таймаут в секундах (опционально)</param>
        /// <param name="_onTimeout">Действие, выполняемое при таймауте (опционально)</param>
        /// <returns>Ссылка на запущенную корутину</returns>
        Coroutine ExecuteUntil(Func<bool> _predicate, Action _onComplete = null, float _timeout = 0, Action _onTimeout = null);
    }
}
