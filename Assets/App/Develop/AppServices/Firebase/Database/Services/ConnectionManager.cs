using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Database.Services
{
    /// <summary>
    /// Класс для управления соединением с Firebase
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        #region Private Fields
        private readonly DatabaseReference _database;
        private readonly DatabaseReference _connectedRef;
        private bool _isConnected;
        private bool _keepConnection;
        private float _connectionCheckInterval = 30f; // Интервал проверки соединения в секундах
        private float _lastConnectionCheck;
        private EventHandler<ValueChangedEventArgs> _connectionCallback;
        private List<Action<bool>> _connectionStateListeners = new List<Action<bool>>();
        
        // Для оптимизации запросов
        private bool _isOfflineCachingEnabled = true;
        private float _disablePersistenceAfterInactivitySeconds = 300f; // 5 минут
        private float _lastActivityTime;
        private System.Threading.CancellationTokenSource _persistenceCts;
        private bool _isPersistenceEnabled = true; // Локальное отслеживание состояния персистентности
        #endregion

        #region Properties
        /// <summary>
        /// Состояние соединения
        /// </summary>
        public bool IsConnected => _isConnected;
        
        /// <summary>
        /// Включен ли режим сохранения соединения
        /// </summary>
        public bool KeepConnection 
        {
            get => _keepConnection;
            set => SetKeepConnection(value);
        }
        
        /// <summary>
        /// Интервал проверки соединения
        /// </summary>
        public float ConnectionCheckInterval
        {
            get => _connectionCheckInterval;
            set => _connectionCheckInterval = Mathf.Max(5f, value);
        }
        
        /// <summary>
        /// Включено ли офлайн кэширование
        /// </summary>
        public bool IsOfflineCachingEnabled
        {
            get => _isOfflineCachingEnabled;
            set => SetOfflineCaching(value);
        }
        
        /// <summary>
        /// Время бездействия в секундах, после которого отключается персистентность (для экономии ресурсов)
        /// </summary>
        public float DisablePersistenceAfterInactivitySeconds
        {
            get => _disablePersistenceAfterInactivitySeconds;
            set => _disablePersistenceAfterInactivitySeconds = Mathf.Max(60f, value);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр менеджера соединений
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="keepConnection">Поддерживать ли соединение постоянно</param>
        public ConnectionManager(DatabaseReference database, bool keepConnection = true)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _connectedRef = database.Root.Child(".info/connected");
            _keepConnection = keepConnection;
            
            _connectionCallback = new EventHandler<ValueChangedEventArgs>(OnConnectionChanged);
            _lastActivityTime = Time.realtimeSinceStartup;
            
            if (keepConnection)
            {
                StartConnectionMonitoring();
            }
            
            Debug.Log("✅ ConnectionManager инициализирован");
        }
        #endregion

        #region Connection Management
        /// <summary>
        /// Включает или выключает режим поддержания соединения
        /// </summary>
        private void SetKeepConnection(bool keep)
        {
            if (_keepConnection == keep)
            {
                return;
            }
            
            _keepConnection = keep;
            
            if (_keepConnection)
            {
                StartConnectionMonitoring();
            }
            else
            {
                StopConnectionMonitoring();
            }
            
            Debug.Log($"ConnectionManager: Режим поддержания соединения {(_keepConnection ? "включен" : "выключен")}");
        }
        
        /// <summary>
        /// Включает наблюдение за состоянием соединения
        /// </summary>
        private void StartConnectionMonitoring()
        {
            _connectedRef.ValueChanged += _connectionCallback;
            Debug.Log("ConnectionManager: Наблюдение за соединением запущено");
        }
        
        /// <summary>
        /// Останавливает наблюдение за состоянием соединения
        /// </summary>
        private void StopConnectionMonitoring()
        {
            _connectedRef.ValueChanged -= _connectionCallback;
            Debug.Log("ConnectionManager: Наблюдение за соединением остановлено");
        }
        
        /// <summary>
        /// Обработчик изменения состояния соединения
        /// </summary>
        private void OnConnectionChanged(object sender, ValueChangedEventArgs e)
        {
            if (e.DatabaseError != null)
            {
                Debug.LogError($"ConnectionManager: Ошибка проверки соединения: {e.DatabaseError.Message}");
                _isConnected = false;
            }
            else if (e.Snapshot != null && e.Snapshot.Exists)
            {
                bool connected = (bool)e.Snapshot.Value;
                if (_isConnected != connected)
                {
                    _isConnected = connected;
                    NotifyConnectionStateChanged(_isConnected);
                    
                    Debug.Log($"ConnectionManager: Состояние соединения изменилось: {(_isConnected ? "подключено" : "отключено")}");
                }
            }
        }
        
        /// <summary>
        /// Добавляет слушателя состояния соединения
        /// </summary>
        /// <param name="listener">Функция обратного вызова</param>
        public void AddConnectionStateListener(Action<bool> listener)
        {
            if (listener == null)
            {
                return;
            }
            
            _connectionStateListeners.Add(listener);
            
            // Сразу вызываем с текущим состоянием
            listener(_isConnected);
        }
        
        /// <summary>
        /// Удаляет слушателя состояния соединения
        /// </summary>
        /// <param name="listener">Функция обратного вызова</param>
        public void RemoveConnectionStateListener(Action<bool> listener)
        {
            if (listener == null)
            {
                return;
            }
            
            _connectionStateListeners.Remove(listener);
        }
        
        /// <summary>
        /// Уведомляет слушателей об изменении состояния соединения
        /// </summary>
        /// <param name="isConnected">Новое состояние соединения</param>
        private void NotifyConnectionStateChanged(bool isConnected)
        {
            foreach (var listener in _connectionStateListeners)
            {
                try
                {
                    listener(isConnected);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ConnectionManager: Ошибка в обработчике состояния соединения: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Проверяет соединение с Firebase
        /// </summary>
        /// <returns>true, если соединение активно, иначе false</returns>
        public async Task<bool> CheckConnection()
        {
            try
            {
                var snapshot = await _connectedRef.GetValueAsync();
                if (snapshot != null && snapshot.Exists)
                {
                    _isConnected = (bool)snapshot.Value;
                }
                else
                {
                    _isConnected = false;
                }
                
                _lastConnectionCheck = Time.realtimeSinceStartup;
                return _isConnected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConnectionManager: Ошибка проверки соединения: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }
        
        /// <summary>
        /// Проверяет, нужно ли выполнить проверку соединения
        /// </summary>
        /// <returns>true, если требуется проверка</returns>
        public bool ShouldCheckConnection()
        {
            return Time.realtimeSinceStartup - _lastConnectionCheck >= _connectionCheckInterval;
        }
        #endregion
        
        #region Offline Caching & Performance Optimization
        /// <summary>
        /// Включает или выключает офлайн кэширование
        /// </summary>
        /// <param name="enable">true для включения, false для отключения</param>
        private void SetOfflineCaching(bool enable)
        {
            if (_isOfflineCachingEnabled == enable)
            {
                return;
            }
            
            _isOfflineCachingEnabled = enable;
            
            try
            {
                if (_isOfflineCachingEnabled)
                {
                    FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                    _isPersistenceEnabled = true;
                    Debug.Log("ConnectionManager: Офлайн кэширование включено");
                }
                else
                {
                    FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
                    _isPersistenceEnabled = false;
                    Debug.Log("ConnectionManager: Офлайн кэширование отключено");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConnectionManager: Ошибка при изменении режима кэширования: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Регистрирует активность пользователя
        /// </summary>
        public void RegisterActivity()
        {
            _lastActivityTime = Time.realtimeSinceStartup;
            
            // Если необходимо, включаем персистентность снова
            if (_isOfflineCachingEnabled && !_isPersistenceEnabled)
            {
                try
                {
                    FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                    _isPersistenceEnabled = true;
                    Debug.Log("ConnectionManager: Персистентность включена после активности пользователя");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ConnectionManager: Ошибка при включении персистентности: {ex.Message}");
                }
            }
            
            // Отменяем предыдущий запланированный таймер отключения
            _persistenceCts?.Cancel();
            _persistenceCts = new System.Threading.CancellationTokenSource();
            
            // Запускаем новый таймер
            var token = _persistenceCts.Token;
            Task.Delay(TimeSpan.FromSeconds(_disablePersistenceAfterInactivitySeconds), token)
                .ContinueWith(t => 
                {
                    if (!t.IsCanceled && _isOfflineCachingEnabled)
                    {
                        // Прошло достаточное время бездействия, отключаем персистентность
                        DisablePersistenceToSaveResources();
                    }
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        /// <summary>
        /// Отключает персистентность для экономии ресурсов
        /// </summary>
        private void DisablePersistenceToSaveResources()
        {
            try
            {
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
                _isPersistenceEnabled = false;
                Debug.Log("ConnectionManager: Персистентность отключена для экономии ресурсов");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConnectionManager: Ошибка при отключении персистентности: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Устанавливает приоритет для сетевого или кэшированного доступа
        /// </summary>
        /// <param name="prioritizeNetwork">true для приоритета сети, false для приоритета кэша</param>
        public void SetNetworkPriority(bool prioritizeNetwork)
        {
            try
            {
                if (prioritizeNetwork)
                {
                    FirebaseDatabase.DefaultInstance.GoOnline();
                    Debug.Log("ConnectionManager: Установлен приоритет сетевого доступа");
                }
                else
                {
                    FirebaseDatabase.DefaultInstance.GoOffline();
                    Debug.Log("ConnectionManager: Установлен приоритет кэшированных данных");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConnectionManager: Ошибка при изменении приоритета доступа: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Устанавливает тайм-аут для сетевых операций
        /// </summary>
        /// <param name="timeoutMs">Тайм-аут в миллисекундах</param>
        public void SetNetworkTimeout(long timeoutMs)
        {
            try
            {
                // Метод SetPersistenceCacheSizeBytes не существует, поэтому просто логируем действие
                Debug.Log($"ConnectionManager: Установка тайм-аута сетевых операций: {timeoutMs} мс " +
                          "(примечание: реальная установка не выполнена из-за ограничений SDK)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConnectionManager: Ошибка при установке тайм-аута: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Устанавливает размер кэша
        /// </summary>
        /// <param name="sizeBytes">Размер кэша в байтах</param>
        public void SetCacheSize(long sizeBytes)
        {
            try
            {
                // В Firebase SDK нет прямого метода установки размера кэша
                // Используем доступный метод SetPersistenceCacheSizeBytes, если он будет добавлен в SDK
                Debug.Log($"ConnectionManager: Установка размера кэша: {sizeBytes} байт " +
                          "(примечание: реальная установка не выполнена из-за ограничений SDK)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConnectionManager: Ошибка при установке размера кэша: {ex.Message}");
            }
        }
        #endregion
        
        #region IDisposable Implementation
        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            StopConnectionMonitoring();
            _connectionStateListeners.Clear();
            _persistenceCts?.Cancel();
            _persistenceCts?.Dispose();
            _persistenceCts = null;
            
            Debug.Log("ConnectionManager: Ресурсы освобождены");
        }
        #endregion
    }
} 