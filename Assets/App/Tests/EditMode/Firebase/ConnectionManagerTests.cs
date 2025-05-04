using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Tests.EditMode.TestHelpers;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests.EditMode.Firebase
{
    [TestFixture]
    public class ConnectionManagerTests
    {
        private MockFirebaseDatabase _mockDatabase;
        private MockConnectionManager _connectionManager;
        
        [SetUp]
        public void Setup()
        {
            Debug.Log("Настройка тестов менеджера соединений...");
            
            // Создаем мок базы данных
            _mockDatabase = MockFirebaseDatabase.GetInstance();
            
            // Создаем мок-менеджер соединений
            _connectionManager = new MockConnectionManager(_mockDatabase);
            
            Debug.Log("Настройка тестов менеджера соединений завершена");
        }
        
        [TearDown]
        public void TearDown()
        {
            Debug.Log("Очистка после тестов менеджера соединений...");
            
            _connectionManager.Dispose();
            _connectionManager = null;
            _mockDatabase = null;
            
            Debug.Log("Очистка после тестов менеджера соединений завершена");
        }
        
        [Test]
        public async Task ConnectionStatus_ShouldReportCorrectly()
        {
            // Имитируем подключение
            _connectionManager.SimulateConnect();
            
            // Проверяем статус
            Assert.IsTrue(_connectionManager.IsConnected);
            
            // Имитируем отключение
            _connectionManager.SimulateDisconnect();
            
            // Проверяем статус
            Assert.IsFalse(_connectionManager.IsConnected);
            
            // Даем время для обработки событий
            await Task.Delay(100);
        }
        
        [Test]
        public void KeepConnectionSetting_ShouldWorkCorrectly()
        {
            // Проверяем изначальное состояние
            Assert.IsFalse(_connectionManager.KeepConnection);
            
            // Включаем поддержание соединения
            _connectionManager.KeepConnection = true;
            Assert.IsTrue(_connectionManager.KeepConnection);
            
            // Выключаем поддержание соединения
            _connectionManager.KeepConnection = false;
            Assert.IsFalse(_connectionManager.KeepConnection);
        }
        
        [Test]
        public void ConnectionCheckInterval_ShouldBeConfigurable()
        {
            // Проверяем изначальное значение
            Assert.AreEqual(30f, _connectionManager.ConnectionCheckInterval);
            
            // Устанавливаем новое значение
            _connectionManager.ConnectionCheckInterval = 60f;
            Assert.AreEqual(60f, _connectionManager.ConnectionCheckInterval);
        }
    }
    
    // Мок-класс для тестирования ConnectionManager
    public class MockConnectionManager : IConnectionManager
    {
        private readonly MockFirebaseDatabase _database;
        private bool _isConnected;
        private bool _keepConnection;
        private bool _isOfflineCachingEnabled = true;
        private float _connectionCheckInterval = 30f;
        private float _disablePersistenceAfterInactivitySeconds = 300f;
        private float _lastActivityTime;
        private float _lastConnectionCheck;
        private long _networkTimeout = 30000; // 30 секунд
        private long _cacheSize = 10485760; // 10 МБ
        
        private Action<bool> _connectionCallback;
        
        public MockConnectionManager(MockFirebaseDatabase database)
        {
            _database = database;
            _isConnected = false;
            _keepConnection = false;
            _lastActivityTime = Time.realtimeSinceStartup;
            _lastConnectionCheck = Time.realtimeSinceStartup;
        }
        
        public bool IsConnected => _isConnected;
        
        public bool KeepConnection 
        { 
            get => _keepConnection; 
            set => _keepConnection = value; 
        }
        
        public float ConnectionCheckInterval 
        { 
            get => _connectionCheckInterval; 
            set => _connectionCheckInterval = Mathf.Max(1f, value); 
        }
        
        public bool IsOfflineCachingEnabled { get; set; } = true;
        
        public float DisablePersistenceAfterInactivitySeconds
        {
            get => _disablePersistenceAfterInactivitySeconds;
            set => _disablePersistenceAfterInactivitySeconds = Mathf.Max(0f, value);
        }
        
        public void Connect()
        {
            _isConnected = true;
        }
        
        public void Disconnect()
        {
            _isConnected = false;
        }
        
        public void SetKeepConnection(bool keep)
        {
            _keepConnection = keep;
        }
        
        public void SetConnectionCheckInterval(float interval)
        {
            _connectionCheckInterval = Mathf.Max(1f, interval);
        }
        
        public void SimulateConnect()
        {
            _isConnected = true;
            _connectionCallback?.Invoke(true);
        }
        
        public void SimulateDisconnect()
        {
            _isConnected = false;
            _connectionCallback?.Invoke(false);
        }
        
        public void AddConnectionStateListener(Action<bool> listener)
        {
            _connectionCallback += listener;
            listener?.Invoke(_isConnected);
        }
        
        public void RemoveConnectionStateListener(Action<bool> listener)
        {
            _connectionCallback -= listener;
        }
        
        public Task<bool> CheckConnection()
        {
            _lastConnectionCheck = Time.realtimeSinceStartup;
            return Task.FromResult(_isConnected);
        }
        
        public bool ShouldCheckConnection()
        {
            return Time.realtimeSinceStartup - _lastConnectionCheck >= _connectionCheckInterval;
        }
        
        public void RegisterActivity()
        {
            _lastActivityTime = Time.realtimeSinceStartup;
        }
        
        public void SetNetworkPriority(bool highPriority)
        {
            // Для тестов просто заглушка
        }
        
        public void SetNetworkTimeout(long milliseconds)
        {
            _networkTimeout = milliseconds;
        }
        
        public void SetCacheSize(long sizeInBytes)
        {
            _cacheSize = sizeInBytes;
        }
        
        public void Dispose()
        {
            Disconnect();
        }
    }
} 