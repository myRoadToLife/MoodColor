using System;
using System.Collections.Generic;
using App.Develop.Utils.Logging;

namespace App.Develop.Utils.Events
{
    /// <summary>
    /// Глобальная шина событий для слабо связанной коммуникации между компонентами
    /// Реализует паттерн Observer для глобальных событий
    /// </summary>
    public class EventBus : IDisposable
    {
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers;
        private bool _isDisposed;

        public EventBus()
        {
            _eventHandlers = new Dictionary<Type, List<Delegate>>();
        }

        /// <summary>
        /// Подписывается на событие определенного типа
        /// </summary>
        /// <typeparam name="T">Тип события</typeparam>
        /// <param name="handler">Обработчик события</param>
        public void Subscribe<T>(EventHandler<T> handler) where T : EventArgs
        {
            if (_isDisposed)
            {
                MyLogger.LogWarning("Попытка подписки на событие в освобожденном EventBus", MyLogger.LogCategory.Default);
                return;
            }

            var eventType = typeof(T);
            
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Delegate>();
            }
            
            _eventHandlers[eventType].Add(handler);
            MyLogger.Log($"🔔 Подписка на событие {typeof(T).Name}", MyLogger.LogCategory.Default);
        }

        /// <summary>
        /// Отписывается от события определенного типа
        /// </summary>
        /// <typeparam name="T">Тип события</typeparam>
        /// <param name="handler">Обработчик события</param>
        public void Unsubscribe<T>(EventHandler<T> handler) where T : EventArgs
        {
            if (_isDisposed) return;

            var eventType = typeof(T);
            
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType].Remove(handler);
                
                if (_eventHandlers[eventType].Count == 0)
                {
                    _eventHandlers.Remove(eventType);
                }
                
                MyLogger.Log($"🔕 Отписка от события {typeof(T).Name}", MyLogger.LogCategory.Default);
            }
        }

        /// <summary>
        /// Публикует событие всем подписчикам
        /// </summary>
        /// <typeparam name="T">Тип события</typeparam>
        /// <param name="eventArgs">Аргументы события</param>
        public void Publish<T>(T eventArgs) where T : EventArgs
        {
            if (_isDisposed) return;

            var eventType = typeof(T);
            
            if (!_eventHandlers.ContainsKey(eventType)) return;

            var handlers = new List<Delegate>(_eventHandlers[eventType]); // Копия для безопасности
            
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is EventHandler<T> typedHandler)
                    {
                        typedHandler.Invoke(this, eventArgs);
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ Ошибка в обработчике события {typeof(T).Name}: {ex.Message}", MyLogger.LogCategory.Default);
                    MyLogger.LogError($"StackTrace: {ex.StackTrace}", MyLogger.LogCategory.Default);
                }
            }
            
            MyLogger.Log($"📢 Публикация события {typeof(T).Name}", MyLogger.LogCategory.Default);
        }

        /// <summary>
        /// Очищает все подписки
        /// </summary>
        public void Clear()
        {
            _eventHandlers.Clear();
            MyLogger.Log("🧹 EventBus очищен", MyLogger.LogCategory.Default);
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            Clear();
            _isDisposed = true;
            MyLogger.Log("🗑️ EventBus disposed", MyLogger.LogCategory.Default);
        }
    }
} 