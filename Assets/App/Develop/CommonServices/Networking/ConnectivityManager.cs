using System;
using UnityEngine;
using UnityEngine.Networking;

namespace App.Develop.CommonServices.Networking
{
    public class ConnectivityManager : MonoBehaviour
    {
        [SerializeField] private float _checkInterval = 30f; // Интервал проверки в секундах
        [SerializeField] private string _connectivityTestUrl = "https://www.google.com";
        
        private bool _isConnected;
        private bool _isWifiConnected;
        private float _lastCheckTime;
        
        public bool IsConnected => _isConnected;
        public bool IsWifiConnected => _isWifiConnected;
        
        public event Action<bool> OnConnectivityChanged;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
        
        private void Start()
        {
            CheckConnectivity();
        }
        
        private void Update()
        {
            if (Time.time - _lastCheckTime > _checkInterval)
            {
                CheckConnectivity();
                _lastCheckTime = Time.time;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Проверяет состояние подключения к сети
        /// </summary>
        public void CheckConnectivity()
        {
            StartCoroutine(CheckInternetConnection((isConnected) =>
            {
                bool previousState = _isConnected;
                _isConnected = isConnected;
                
                // Проверяем тип соединения
                CheckConnectionType();
                
                // Вызываем событие, если состояние изменилось
                if (previousState != _isConnected)
                {
                    OnConnectivityChanged?.Invoke(_isConnected);
                    Debug.Log($"Состояние соединения изменилось: {_isConnected}, WiFi: {_isWifiConnected}");
                }
            }));
        }
        
        /// <summary>
        /// Проверяет текущий статус подключения (синхронно)
        /// </summary>
        public NetworkReachability GetConnectionType()
        {
            return Application.internetReachability;
        }
        
        /// <summary>
        /// Проверяет доступность конкретного сервера
        /// </summary>
        public void CheckServerAvailability(string url, Action<bool> callback)
        {
            StartCoroutine(CheckInternetConnection(callback, url));
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Проверяет тип подключения (WiFi, сотовая связь, нет подключения)
        /// </summary>
        private void CheckConnectionType()
        {
            NetworkReachability reachability = Application.internetReachability;
            
            _isWifiConnected = reachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            
            string connectionType = reachability switch
            {
                NetworkReachability.NotReachable => "Нет подключения",
                NetworkReachability.ReachableViaCarrierDataNetwork => "Подключено через сотовую сеть",
                NetworkReachability.ReachableViaLocalAreaNetwork => "Подключено через WiFi",
                _ => "Неизвестный тип подключения"
            };
            
            Debug.Log($"Тип подключения: {connectionType}");
        }
        
        /// <summary>
        /// Корутина для проверки подключения к интернету
        /// </summary>
        private System.Collections.IEnumerator CheckInternetConnection(Action<bool> callback, string url = null)
        {
            string testUrl = string.IsNullOrEmpty(url) ? _connectivityTestUrl : url;
            
            using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
            {
                request.timeout = 5; // Таймаут в секундах
                
                yield return request.SendWebRequest();
                
                bool isConnected = !(request.result == UnityWebRequest.Result.ConnectionError || 
                                     request.result == UnityWebRequest.Result.ProtocolError);
                
                callback?.Invoke(isConnected);
            }
        }
        
        #endregion
    }
} 