using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Инициализатор Firebase с упрощенной логикой и offline capabilities
    /// </summary>
    public class FirebaseInitializer : IFirebaseInitializer
    {
        #region Fields

        private bool _isConnected = false;
        private DatabaseReference _connectedRef;
        private IOfflineManager _offlineManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор FirebaseInitializer
        /// </summary>
        public FirebaseInitializer()
        {
            // OfflineManager будет внедрен позже через SetOfflineManager
        }

        /// <summary>
        /// Устанавливает OfflineManager для синхронизации состояния подключения
        /// </summary>
        /// <param name="offlineManager">Менеджер offline операций</param>
        public void SetOfflineManager(IOfflineManager offlineManager)
        {
            _offlineManager = offlineManager;
            MyLogger.Log("✅ [FirebaseInitializer] OfflineManager установлен", MyLogger.LogCategory.Bootstrap);
        }

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения состояния подключения
        /// </summary>
        public event System.Action<bool> ConnectionStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Получает состояние подключения к Firebase
        /// </summary>
        public bool IsConnected => _isConnected;

        #endregion

        #region Public Methods

        /// <summary>
        /// Асинхронно инициализирует Firebase с использованием лучших практик
        /// </summary>
        /// <returns>True, если инициализация прошла успешно</returns>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                MyLogger.Log("🔥 [FirebaseInitializer] Начинаем инициализацию Firebase...", MyLogger.LogCategory.Bootstrap);

                MyLogger.Log("🔍 [FirebaseInitializer] Проверяем зависимости Firebase...", MyLogger.LogCategory.Bootstrap);
                // Проверяем зависимости Firebase
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

                if (dependencyStatus != DependencyStatus.Available)
                {
                    MyLogger.LogError($"❌ [FirebaseInitializer] Firebase зависимости недоступны: {dependencyStatus}", MyLogger.LogCategory.Bootstrap);
                    return false;
                }

                MyLogger.Log("✅ [FirebaseInitializer] Firebase зависимости проверены успешно", MyLogger.LogCategory.Bootstrap);

                MyLogger.Log("🔍 [FirebaseInitializer] Получаем default instance Firebase Database...", MyLogger.LogCategory.Bootstrap);
                // Используем default app (лучшая практика)
                // Firebase автоматически инициализируется из google-services.json
                var defaultInstance = FirebaseDatabase.DefaultInstance;

                if (defaultInstance == null)
                {
                    MyLogger.LogError("❌ [FirebaseInitializer] FirebaseDatabase.DefaultInstance вернул null", MyLogger.LogCategory.Bootstrap);
                    return false;
                }

                MyLogger.Log("✅ [FirebaseInitializer] Firebase Database DefaultInstance получен", MyLogger.LogCategory.Bootstrap);

                MyLogger.Log("🔍 [FirebaseInitializer] Включаем persistence...", MyLogger.LogCategory.Bootstrap);
                // Включаем persistence для offline работы
                defaultInstance.SetPersistenceEnabled(true);
                MyLogger.Log("✅ [FirebaseInitializer] Firebase persistence включен", MyLogger.LogCategory.Bootstrap);

                MyLogger.Log("🔍 [FirebaseInitializer] Настраиваем offline capabilities...", MyLogger.LogCategory.Bootstrap);
                // Настраиваем offline capabilities
                SetupOfflineCapabilities();

                MyLogger.Log("✅ [FirebaseInitializer] Firebase инициализирован успешно (используя default app)", MyLogger.LogCategory.Bootstrap);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [FirebaseInitializer] Критическая ошибка инициализации Firebase: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                MyLogger.LogError($"❌ [FirebaseInitializer] StackTrace: {ex.StackTrace}", MyLogger.LogCategory.Bootstrap);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Настраивает offline capabilities для критичных данных
        /// </summary>
        private void SetupOfflineCapabilities()
        {
            try
            {
                // keepSynced для критичных данных
                var database = FirebaseDatabase.DefaultInstance;

                // Синхронизируем критичные узлы данных
                database.GetReference("users").KeepSynced(true);
                database.GetReference("emotions").KeepSynced(true);
                database.GetReference("jars").KeepSynced(true);

                MyLogger.Log("✅ KeepSynced настроен для критичных данных", MyLogger.LogCategory.Bootstrap);

                // Мониторинг состояния подключения
                SetupConnectionStateMonitoring();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка настройки offline capabilities: {ex.Message}", MyLogger.LogCategory.Bootstrap);
            }
        }

        /// <summary>
        /// Настраивает мониторинг состояния подключения
        /// </summary>
        private void SetupConnectionStateMonitoring()
        {
            try
            {
                // Подписываемся на изменения состояния подключения
                _connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
                _connectedRef.ValueChanged += OnConnectionStateChanged;

                MyLogger.Log("✅ Мониторинг состояния подключения настроен", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка настройки мониторинга подключения: {ex.Message}", MyLogger.LogCategory.Bootstrap);
            }
        }

        /// <summary>
        /// Обработчик изменения состояния подключения
        /// </summary>
        private void OnConnectionStateChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                MyLogger.LogError($"❌ Ошибка мониторинга подключения: {args.DatabaseError.Message}", MyLogger.LogCategory.Firebase);
                return;
            }

            bool connected = args.Snapshot.Value != null && (bool)args.Snapshot.Value;

            if (_isConnected != connected)
            {
                _isConnected = connected;

                if (connected)
                {
                    MyLogger.Log("🟢 Firebase подключение восстановлено", MyLogger.LogCategory.Firebase);
                }
                else
                {
                    MyLogger.Log("🔴 Firebase подключение потеряно", MyLogger.LogCategory.Firebase);
                }

                // Синхронизируем состояние с OfflineManager
                if (_offlineManager != null)
                {
                    _offlineManager.UpdateConnectionState(_isConnected);
                }

                // Уведомляем подписчиков об изменении состояния
                ConnectionStateChanged?.Invoke(_isConnected);
            }
        }

        #endregion
    }
}