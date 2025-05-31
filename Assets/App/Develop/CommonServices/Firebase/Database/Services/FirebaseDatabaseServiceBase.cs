using System;
using System.Collections.Generic;
using Firebase.Database;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Common.Helpers;
using App.Develop.Utils.Logging;
using Newtonsoft.Json;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Базовый абстрактный класс для сервисов Firebase Database
    /// </summary>
    public abstract class FirebaseDatabaseServiceBase : IDisposable
    {
        #region Protected Fields
        /// <summary>
        /// Ссылка на базу данных
        /// </summary>
        protected readonly DatabaseReference _database;
        
        /// <summary>
        /// Менеджер кэша Firebase
        /// </summary>
        protected readonly FirebaseCacheManager _cacheManager;
        
        /// <summary>
        /// Менеджер пакетных операций
        /// </summary>
        protected readonly FirebaseBatchManager _batchManager;
        
        /// <summary>
        /// Сервис валидации данных
        /// </summary>
        protected readonly DataValidationService _validationService;
        
        /// <summary>
        /// ID текущего пользователя
        /// </summary>
        protected string _userId;
        
        /// <summary>
        /// Список активных слушателей для отписки
        /// </summary>
        protected readonly List<DatabaseReference> _activeListeners = new List<DatabaseReference>();
        
        /// <summary>
        /// Словарь для хранения ссылок на обработчики событий для корректной отписки
        /// </summary>
        protected readonly Dictionary<DatabaseReference, EventHandler<ValueChangedEventArgs>> _eventHandlers =
            new Dictionary<DatabaseReference, EventHandler<ValueChangedEventArgs>>();
            
        /// <summary>
        /// Флаг, указывающий, что сейчас происходит обновление UserId, 
        /// чтобы избежать циклических вызовов
        /// </summary>
        protected bool _isUpdatingUserId = false;
        #endregion

        #region Events
        /// <summary>
        /// Событие вызывается при изменении ID пользователя
        /// </summary>
        public event Action<string> UserIdChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Ссылка на корень базы данных
        /// </summary>
        public DatabaseReference RootReference => _database;
        
        /// <summary>
        /// ID текущего пользователя
        /// </summary>
        public string UserId => _userId;

        /// <summary>
        /// Проверяет, аутентифицирован ли пользователь
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
        
        /// <summary>
        /// Менеджер пакетных операций
        /// </summary>
        public FirebaseBatchManager BatchManager => _batchManager;
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр базового сервиса базы данных
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        protected FirebaseDatabaseServiceBase(
            DatabaseReference database, 
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _validationService = validationService; // Может быть null
            _batchManager = new FirebaseBatchManager(_database);
            
            // Подписываемся на события завершения батча
            _batchManager.OnBatchCompleted += OnBatchCompleted;
            
            MyLogger.Log("✅ Firebase Database Service инициализирован", MyLogger.LogCategory.Firebase);
        }
        
        private void OnBatchCompleted(bool success, string message)
        {
            if (success)
            {
                MyLogger.Log($"✅ Батч успешно выполнен: {message}", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.LogError($"❌ Ошибка выполнения батча: {message}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion

        /// <summary>
        /// Обновляет ID пользователя при аутентификации
        /// </summary>
        public virtual void UpdateUserId(string userId)
        {
            // Предотвращение циклических вызовов
            if (_isUpdatingUserId)
            {
                MyLogger.LogWarning("🔄 [DATABASE-AUTH] Предотвращен циклический вызов UpdateUserId", MyLogger.LogCategory.Firebase);
                return;
            }
            
            _isUpdatingUserId = true;
            
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    MyLogger.LogWarning("🔑 [DATABASE-AUTH] ⚠️ Попытка установить пустой UserId", MyLogger.LogCategory.Firebase);
                    _userId = string.Empty;
                    
                    // Вызываем событие только если ID действительно изменился
                    MyLogger.Log($"🔑 [DATABASE-AUTH] 📢 Вызываем событие UserIdChanged с пустым ID", MyLogger.LogCategory.Firebase);
                    UserIdChanged?.Invoke(_userId);
                    
                    return;
                }

                // Проверяем, изменился ли идентификатор
                bool hasChanged = _userId != userId;
                
                if (hasChanged)
                {
                    _userId = userId;
                    
                    MyLogger.Log($"🔑 [DATABASE-AUTH] 🔄 UserId обновлен: {_userId.Substring(0, Math.Min(8, _userId.Length))}...", MyLogger.LogCategory.Firebase);
                    MyLogger.Log($"🔑 [DATABASE-AUTH] 📢 Вызываем событие UserIdChanged с новым ID", MyLogger.LogCategory.Firebase);
                    UserIdChanged?.Invoke(_userId);
                }
                else
                {
                    MyLogger.Log($"🔑 [DATABASE-AUTH] ℹ️ UserId не изменился, событие не вызывается", MyLogger.LogCategory.Firebase);
                }
            }
            finally
            {
                _isUpdatingUserId = false;
            }
        }

        /// <summary>
        /// Проверяет, аутентифицирован ли пользователь (установлен ли _userId)
        /// </summary>
        protected bool CheckAuthentication()
        {
            if (string.IsNullOrEmpty(_userId))
            {
                MyLogger.LogWarning("⚠️ Операция требует авторизации пользователя", MyLogger.LogCategory.Firebase);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Базовый метод для подписки на события ValueChanged
        /// </summary>
        protected void SubscribeToData<T>(DatabaseReference reference, Action<T> onUpdate)
        {
            if (_eventHandlers.ContainsKey(reference))
            {
                MyLogger.LogWarning($"Попытка повторной подписки на {reference.Key}", MyLogger.LogCategory.Firebase);
                return; // Уже подписаны
            }

            _activeListeners.Add(reference);

            EventHandler<ValueChangedEventArgs> handler = (sender, args) =>
            {
                if (args.DatabaseError != null)
                {
                    MyLogger.LogError($"Ошибка Firebase при прослушивании {reference.Key}: {args.DatabaseError.Message}", MyLogger.LogCategory.Firebase);
                    return;
                }

                if (args.Snapshot?.Exists == true && args.Snapshot.Value != null)
                {
                    try
                    {
                        // Десериализация с помощью Newtonsoft.Json
                        var json = JsonConvert.SerializeObject(args.Snapshot.Value);
                        var data = JsonConvert.DeserializeObject<T>(json);
                        onUpdate?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ Ошибка обработки данных для {reference.Key}: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                    }
                }
                else
                {
                    MyLogger.Log($"Данные для {reference.Key} не найдены или пусты.", MyLogger.LogCategory.Firebase);
                    // Вызываем onUpdate с default(T), чтобы обработать случай отсутствия данных
                    onUpdate?.Invoke(default(T));
                }
            };

            _eventHandlers[reference] = handler; // Сохраняем обработчик
            reference.ValueChanged += handler; // Подписываемся
            MyLogger.Log($"Подписка на {reference.Key} установлена.", MyLogger.LogCategory.Firebase);
        }

        /// <summary>
        /// Проверяет подключение к базе данных
        /// </summary>
        public async System.Threading.Tasks.Task<bool> CheckConnection()
        {
            try
            {
                // Проверяем подключение, запрашивая специальный узел
                var connectionRef = _database.Root.Child(".info/connected");
                var snapshot = await connectionRef.GetValueAsync();
                
                bool isConnected = snapshot.Exists && snapshot.Value != null && (bool)snapshot.Value;
                
                MyLogger.Log($"Статус подключения к Firebase: {(isConnected ? "Подключено" : "Не подключено", MyLogger.LogCategory.Firebase)}");
                return isConnected;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки подключения: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Освобождает ресурсы (отписка от событий)
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                MyLogger.Log($"Disposing FirebaseDatabaseServiceBase. Отписка от {_eventHandlers.Count} слушателей...", MyLogger.LogCategory.Firebase);
                
                // Обходим копию ключей, чтобы избежать проблем при изменении словаря во время итерации
                var referencesToUnsubscribe = new List<DatabaseReference>(_eventHandlers.Keys);

                foreach (var reference in referencesToUnsubscribe)
                {
                    if (_eventHandlers.TryGetValue(reference, out var handler))
                    {
                        reference.ValueChanged -= handler; // Отписываемся
                        MyLogger.Log($"Отписка от {reference.Key} выполнена.", MyLogger.LogCategory.Firebase);
                    }
                }

                _eventHandlers.Clear(); // Очищаем словарь обработчиков
                _activeListeners.Clear(); // Очищаем список активных ссылок
                
                // Отписываемся от событий FirebaseBatchManager
                if (_batchManager != null)
                {
                    _batchManager.OnBatchCompleted -= OnBatchCompleted;
                    
                    // Если есть незавершенные операции батчинга, выполняем их синхронно перед закрытием
                    int pendingCount = _batchManager.GetPendingOperationsCount();
                    if (pendingCount > 0)
                    {
                        MyLogger.Log($"Завершение {pendingCount} незавершенных операций батчинга перед закрытием...", MyLogger.LogCategory.Firebase);
                        try
                        {
                            // Выполняем синхронно, чтобы не потерять данные
                            _batchManager.ExecuteBatchAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            MyLogger.LogError($"❌ Ошибка при выполнении незавершенных операций батчинга: {ex.Message}", MyLogger.LogCategory.Firebase);
                        }
                    }
                }
                
                MyLogger.Log("✅ FirebaseDatabaseServiceBase: все обработчики событий удалены и ресурсы освобождены.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при освобождении ресурсов FirebaseDatabaseServiceBase: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }
    }
} 