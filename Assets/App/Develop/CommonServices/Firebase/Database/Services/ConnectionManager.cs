using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Класс для управления соединением с Firebase
    /// </summary>
    public class ConnectionManager : IConnectionManager, IDisposable
    {
        #region Private Fields
        private readonly DatabaseReference _database;
        private readonly DatabaseReference _connectedRef;
        private bool _isConnected;
        private bool _keepConnectionAlive;
        private float _connectionCheckIntervalSeconds = 30f;
        private float _lastConnectionCheckTime;
        private EventHandler<ValueChangedEventArgs> _connectionChangedCallback;
        private List<Action<bool>> _connectionStateListeners = new List<Action<bool>>();
        
        private bool _isOfflineCachingEnabled = true;
        private float _disablePersistenceAfterInactivitySeconds = 300f;
        private float _lastActivityTime;
        private System.Threading.CancellationTokenSource _persistenceCts;
        private bool _isPersistenceCurrentlyEnabled = true;
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
            get => _keepConnectionAlive;
            set => SetKeepConnectionAlive(value);
        }
        
        /// <summary>
        /// Для обратной совместимости с интерфейсом
        /// </summary>
        bool IConnectionManager.KeepConnection
        {
            get => KeepConnection;
            set => KeepConnection = value;
        }
        
        /// <summary>
        /// Интервал проверки соединения в секундах
        /// </summary>
        public float ConnectionCheckIntervalSeconds
        {
            get => _connectionCheckIntervalSeconds;
            set => _connectionCheckIntervalSeconds = Mathf.Max(5f, value);
        }
        
        /// <summary>
        /// Для обратной совместимости с интерфейсом
        /// </summary>
        float IConnectionManager.ConnectionCheckInterval
        {
            get => ConnectionCheckIntervalSeconds;
            set => ConnectionCheckIntervalSeconds = value;
        }
        
        /// <summary>
        /// Включено ли офлайн кэширование
        /// </summary>
        public bool IsOfflineCachingEnabled
        {
            get => _isOfflineCachingEnabled;
            set => SetOfflineCachingEnabled(value);
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
        /// <param name="keepConnectionAliveFlag">Поддерживать ли соединение постоянно</param>
        public ConnectionManager(DatabaseReference database, bool keepConnectionAliveFlag = true)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _connectedRef = database.Root.Child(".info/connected");
            _keepConnectionAlive = keepConnectionAliveFlag;
            
            _connectionChangedCallback = new EventHandler<ValueChangedEventArgs>(OnConnectionStatusChanged);
            _lastActivityTime = Time.realtimeSinceStartup;
            
            if (_keepConnectionAlive)
            {
                StartConnectionMonitoring();
            }
        }
        #endregion

        #region Connection Management
        /// <summary>
        /// Включает или выключает режим поддержания соединения
        /// </summary>
        private void SetKeepConnectionAlive(bool keepAlive)
        {
            if (_keepConnectionAlive == keepAlive)
            {
                return;
            }
            
            _keepConnectionAlive = keepAlive;
            
            if (_keepConnectionAlive)
            {
                StartConnectionMonitoring();
            }
            else
            {
                StopConnectionMonitoring();
            }
        }
        
        /// <summary>
        /// Включает наблюдение за состоянием соединения
        /// </summary>
        private void StartConnectionMonitoring()
        {
            _connectedRef.ValueChanged += _connectionChangedCallback;
        }
        
        /// <summary>
        /// Останавливает наблюдение за состоянием соединения
        /// </summary>
        private void StopConnectionMonitoring()
        {
            _connectedRef.ValueChanged -= _connectionChangedCallback;
        }
        
        /// <summary>
        /// Обработчик изменения состояния соединения
        /// </summary>
        private void OnConnectionStatusChanged(object sender, ValueChangedEventArgs eventArgs)
        {
            if (eventArgs.DatabaseError != null)
            {
                _isConnected = false;
                throw new Exception($"ConnectionManager: Ошибка проверки соединения: {eventArgs.DatabaseError.Message}", eventArgs.DatabaseError.ToException());
            }
            else if (eventArgs.Snapshot != null && eventArgs.Snapshot.Exists)
            {
                bool currentlyConnected = (bool)eventArgs.Snapshot.Value;
                if (_isConnected != currentlyConnected)
                {
                    _isConnected = currentlyConnected;
                    NotifyConnectionStateChanged(_isConnected);
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
        /// <param name="isConnectedStatus">Новое состояние соединения</param>
        private void NotifyConnectionStateChanged(bool isConnectedStatus)
        {
            foreach (Action<bool> listener in new List<Action<bool>>(_connectionStateListeners))
            {
                try
                {
                    listener(isConnectedStatus);
                }
                catch (Exception ex)
                {
                    throw new Exception($"ConnectionManager: Ошибка в обработчике состояния соединения: {ex.Message}", ex);
                }
            }
        }
        
        /// <summary>
        /// Проверяет соединение с Firebase
        /// </summary>
        /// <returns>true, если соединение активно, иначе false</returns>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                DataSnapshot snapshot = await _connectedRef.GetValueAsync();
                if (snapshot != null && snapshot.Exists)
                {
                    _isConnected = (bool)snapshot.Value;
                }
                else
                {
                    _isConnected = false;
                }
                
                _lastConnectionCheckTime = Time.realtimeSinceStartup;
                return _isConnected;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"ConnectionManager: Ошибка проверки соединения: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Проверяет, нужно ли выполнить проверку соединения
        /// </summary>
        /// <returns>true, если требуется проверка</returns>
        public bool ShouldCheckConnection()
        {
            return Time.realtimeSinceStartup - _lastConnectionCheckTime >= _connectionCheckIntervalSeconds;
        }
        #endregion
        
        #region Offline Caching & Performance Optimization
        /// <summary>
        /// Включает или выключает офлайн кэширование
        /// </summary>
        /// <param name="enable">true для включения, false для отключения</param>
        private void SetOfflineCachingEnabled(bool enable)
        {
            if (_isOfflineCachingEnabled == enable)
            {
                return;
            }
            
            _isOfflineCachingEnabled = enable;
            
            try
            {
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(_isOfflineCachingEnabled);
                _isPersistenceCurrentlyEnabled = _isOfflineCachingEnabled;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ConnectionManager: Ошибка при изменении режима кэширования: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Регистрирует активность пользователя
        /// </summary>
        public void RegisterActivity()
        {
            _lastActivityTime = Time.realtimeSinceStartup;
            
            if (_isOfflineCachingEnabled && !_isPersistenceCurrentlyEnabled)
            {
                try
                {
                    FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                    _isPersistenceCurrentlyEnabled = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ConnectionManager: Ошибка при включении персистентности: {ex.Message}", ex);
                }
            }
            
            _persistenceCts?.Cancel();
            _persistenceCts?.Dispose();
            _persistenceCts = new System.Threading.CancellationTokenSource();
            System.Threading.CancellationToken token = _persistenceCts.Token;

            Task.Delay(TimeSpan.FromSeconds(_disablePersistenceAfterInactivitySeconds), token)
                .ContinueWith(task =>
                {
                    if (!task.IsCanceled && _isOfflineCachingEnabled && _isPersistenceCurrentlyEnabled)
                    {
                        DisablePersistenceToSaveResources();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        /// <summary>
        /// Отключает персистентность для экономии ресурсов
        /// </summary>
        private void DisablePersistenceToSaveResources()
        {
            try
            {
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
                _isPersistenceCurrentlyEnabled = false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ConnectionManager: Ошибка при отключении персистентности: {ex.Message}", ex);
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
                }
                else
                {
                    FirebaseDatabase.DefaultInstance.GoOffline();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ConnectionManager: Ошибка при изменении приоритета доступа: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Устанавливает тайм-аут для сетевых операций (концептуально, SDK не предоставляет)
        /// </summary>
        /// <param name="timeoutMilliseconds">Тайм-аут в миллисекундах</param>
        public void SetNetworkTimeout(long timeoutMilliseconds)
        {
            // Firebase SDK for Unity does not offer a direct way to set network timeouts for RTDB operations.
            // This method remains as a placeholder or for future SDK enhancements.
        }
        
        /// <summary>
        /// Устанавливает размер кэша (концептуально, SDK не предоставляет прямого контроля)
        /// </summary>
        /// <param name="sizeBytes">Размер кэша в байтах</param>
        public void SetCacheSize(long sizeBytes)
        {
            // Firebase SDK for Unity manages cache size internally. SetPersistenceCacheSizeBytes is not a public API.
            // This method remains as a placeholder.
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
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// Для обратной совместимости с интерфейсом
        /// </summary>
        async Task<bool> IConnectionManager.CheckConnection()
        {
            return await CheckConnectionAsync();
        }
    }
} 