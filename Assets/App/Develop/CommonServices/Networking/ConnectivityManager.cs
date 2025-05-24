using System;
using System.Collections;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.Utils.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace App.Develop.CommonServices.Networking
{
    /// <summary>
    /// Сервис для проверки состояния сетевого подключения
    /// </summary>
    public class ConnectivityManager : IDisposable
    {
        private readonly ICoroutinePerformer _coroutinePerformer;

        private float _checkInterval = 30f; // Интервал проверки в секундах
        private string _connectivityTestUrl = "https://www.google.com";

        private bool _isConnected;
        private bool _isWifiConnected;
        private float _lastCheckTime;
        private bool _isRunning;
        private Coroutine _checkCoroutine;

        public bool IsConnected => _isConnected;
        public bool IsWifiConnected => _isWifiConnected;

        public event Action<bool> OnConnectivityChanged;

        /// <summary>
        /// Создает экземпляр менеджера подключения
        /// </summary>
        /// <param name="coroutinePerformer">Исполнитель корутин</param>
        public ConnectivityManager(ICoroutinePerformer coroutinePerformer)
        {
            _coroutinePerformer = coroutinePerformer ?? throw new ArgumentNullException(nameof(coroutinePerformer));

            // Выполняем начальную инициализацию и запускаем проверки
            Initialize();
        }

        /// <summary>
        /// Инициализирует систему проверки подключения
        /// </summary>
        public void Initialize()
        {
            _lastCheckTime = Time.time;
            StartMonitoring();
            CheckConnectivity();
        }

        /// <summary>
        /// Освобождает занятые ресурсы
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
        }

        /// <summary>
        /// Начинает мониторинг состояния сети
        /// </summary>
        public void StartMonitoring()
        {
            if (_isRunning) return;

            _isRunning = true;
            _checkCoroutine = _coroutinePerformer.StartCoroutine(MonitorConnectivity());
        }

        /// <summary>
        /// Останавливает мониторинг состояния сети
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isRunning) return;

            _isRunning = false;

            if (_checkCoroutine != null)
            {
                _coroutinePerformer.StopCoroutine(_checkCoroutine);
                _checkCoroutine = null;
            }
        }

        /// <summary>
        /// Корутина мониторинга подключения
        /// </summary>
        private IEnumerator MonitorConnectivity()
        {
            while (_isRunning)
            {
                yield return new WaitForSeconds(_checkInterval);
                CheckConnectivity();
            }
        }

        /// <summary>
        /// Проверяет состояние подключения к сети
        /// </summary>
        public void CheckConnectivity()
        {
            _coroutinePerformer.StartCoroutine(CheckInternetConnection((isConnected) =>
            {
                bool previousState = _isConnected;
                _isConnected = isConnected;

                // Проверяем тип соединения
                CheckConnectionType();

                // Вызываем событие, если состояние изменилось
                if (previousState != _isConnected)
                {
                    OnConnectivityChanged?.Invoke(_isConnected);
                    MyLogger.Log($"Состояние соединения изменилось: {_isConnected}, WiFi: {_isWifiConnected}", MyLogger.LogCategory.Network);
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
            _coroutinePerformer.StartCoroutine(CheckInternetConnection(callback, url));
        }

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

            MyLogger.Log($"Тип подключения: {connectionType}", MyLogger.LogCategory.Network);
        }

        /// <summary>
        /// Корутина для проверки подключения к интернету
        /// </summary>
        private IEnumerator CheckInternetConnection(Action<bool> callback, string url = null)
        {
            string testUrl = string.IsNullOrEmpty(url) ? _connectivityTestUrl : url;

            using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
            {
                request.timeout = 5;
                // Таймаут в секундах

                yield return request.SendWebRequest();

                bool isConnected = !(request.result == UnityWebRequest.Result.ConnectionError ||
                                     request.result == UnityWebRequest.Result.ProtocolError);

                callback?.Invoke(isConnected);
            }
        }
    }
}
