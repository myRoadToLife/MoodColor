// Assets/App/Develop/AppServices/Firebase/Database/Services/DatabaseService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using Firebase.Database;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Common.Helpers;
using App.Develop.CommonServices.Firebase.Database.Models;
using UnityEngine;
using Firebase.Extensions;
using Firebase;
using Firebase.Auth;
using Newtonsoft.Json;
using UserProfile = App.Develop.CommonServices.Firebase.Database.Models.UserProfile;
using EmotionData = App.Develop.CommonServices.DataManagement.DataProviders.EmotionData;
using EmotionEventType = App.Develop.CommonServices.Emotion.EmotionEventType;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для работы с базой данных Firebase
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        #region Private Fields
        private readonly DatabaseReference _database;
        private readonly FirebaseCacheManager _cacheManager;
        private readonly EmotionHistoryCache _emotionHistoryCache;
        private readonly DataValidationService _validationService;
        private readonly FirebaseBatchManager _batchManager;
        private string _userId;
        private readonly List<DatabaseReference> _activeListeners = new List<DatabaseReference>();

        // Словарь для хранения ссылок на обработчики событий для корректной отписки
        private readonly Dictionary<DatabaseReference, EventHandler<ValueChangedEventArgs>> _eventHandlers =
            new Dictionary<DatabaseReference, EventHandler<ValueChangedEventArgs>>();
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
        /// Создает новый экземпляр сервиса базы данных
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public DatabaseService(
            DatabaseReference database, 
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _emotionHistoryCache = new EmotionHistoryCache(cacheManager);
            _validationService = validationService; // Может быть null
            _batchManager = new FirebaseBatchManager(_database);
            
            // Подписываемся на события завершения батча
            _batchManager.OnBatchCompleted += OnBatchCompleted;
            
            MyLogger.Log("✅ DatabaseService инициализирован", MyLogger.LogCategory.Firebase);
            
            if (_validationService == null)
            {
                MyLogger.LogWarning("⚠️ Сервис валидации данных не предоставлен. Валидация будет отключена!", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.Log("✅ Валидация данных включена в DatabaseService", MyLogger.LogCategory.Firebase);
            }
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

        // Метод для обновления ID пользователя при аутентификации
        public void UpdateUserId(string userId)
        {
            _userId = userId;
            MyLogger.Log($"ID пользователя в DatabaseService обновлен: {userId}", MyLogger.LogCategory.Firebase);
        }

        // Проверка, аутентифицирован ли пользователь (установлен ли _userId)
        private bool CheckAuthentication()
        {
            if (string.IsNullOrEmpty(_userId))
            {
                MyLogger.LogWarning("⚠️ Операция требует авторизации пользователя", MyLogger.LogCategory.Firebase);
                return false;
            }

            return true;
        }

        // Создание записи для нового пользователя в базе данных
        public async Task CreateNewUser(string userId, string email)
        {
            try
            {
                var userRef = _database.Child("users").Child(userId);
                var userSnapshot = await userRef.GetValueAsync();

                if (userSnapshot.Exists)
                {
                    MyLogger.LogWarning($"👤 Пользователь {email} (ID: {userId}, MyLogger.LogCategory.Firebase) уже существует");
                    return;
                }

                // --- Профиль ---
                var settings = new UserSettings
                {
                    Notifications = true,
                    Theme = "default",
                    Sound = true
                };

                var profileData = new Dictionary<string, object>
                {
                    ["email"] = email,
                    ["createdAt"] = ServerValue.Timestamp,
                    ["lastActive"] = ServerValue.Timestamp,
                    ["totalPoints"] = 0,
                    ["settings"] = settings.ToDictionary()
                };

                // --- Баночки ---
                var jarsData = new Dictionary<string, object>();

                foreach (var emotionType in Enum.GetNames(typeof(EmotionTypes)))
                {
                    string key = emotionType.ToLower();

                    var jar = new JarData
                    {
                        Type = emotionType,
                        Level = 1,
                        Capacity = 100,
                        CurrentAmount = 0,
                        Customization = new JarCustomization()
                    };

                    jarsData[key] = jar.ToDictionary();
                }

                // --- Финальные данные ---
                var userData = new Dictionary<string, object>
                {
                    ["profile"] = profileData,
                    ["jars"] = jarsData
                };

                await userRef.UpdateChildrenAsync(userData);
                MyLogger.Log($"✅ Профиль пользователя {email} (ID: {userId}, MyLogger.LogCategory.Firebase) создан");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка создания пользователя (ID: {userId}, MyLogger.LogCategory.Firebase): {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        // Получение профиля пользователя
        public async Task<UserProfile> GetUserProfile(string userId = null)
        {
            string targetUserId = userId ?? _userId; // Используем переданный ID или ID текущего пользователя

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("⚠️ ID пользователя не указан для получения профиля", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(targetUserId).Child("profile").GetValueAsync();

                if (snapshot.Exists && snapshot.Value != null)
                {
                    // Используем Newtonsoft.Json для десериализации из словаря/JSON
                    var json = JsonConvert.SerializeObject(snapshot.Value);
                    return JsonConvert.DeserializeObject<UserProfile>(json);
                }

                MyLogger.LogWarning($"Профиль для пользователя {targetUserId} не найден.", MyLogger.LogCategory.Firebase);
                return null;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения профиля (ID: {targetUserId}, MyLogger.LogCategory.Firebase): {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        // Обновление произвольных данных пользователя
        public async Task UpdateUserData(Dictionary<string, object> updates)
        {
            if (!CheckAuthentication())
            {
                throw new InvalidOperationException("Пользователь не авторизован для обновления данных");
            }

            try
            {
                await _database.Child("users").Child(_userId).UpdateChildrenAsync(updates);
                MyLogger.Log($"✅ Данные пользователя {_userId} обновлены.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления данных пользователя {_userId}: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        // --- Методы прослушивания (Listeners) ---

        // Базовый метод для подписки на события ValueChanged
        private void SubscribeToData <T>(DatabaseReference reference, Action<T> onUpdate)
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
                        // Можно добавить логику уведомления пользователя или отписки при критической ошибке
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

        // Прослушивание эмоций в регионе
        public void ListenToRegionEmotions(string regionId, Action<Dictionary<string, int>> onUpdate)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                MyLogger.LogWarning("⚠️ ID региона не может быть пустым для ListenToRegionEmotions", MyLogger.LogCategory.Firebase);
                return;
            }

            var reference = _database.Child("regions").Child(regionId).Child("emotions");
            SubscribeToData(reference, onUpdate);
        }

        // Прослушивание данных баночек пользователя
        public void ListenToJars(Action<Dictionary<string, JarData>> onUpdate)
        {
            if (!CheckAuthentication()) return;
            var reference = _database.Child("users").Child(_userId).Child("jars");
            SubscribeToData(reference, onUpdate);
        }

        // Прослушивание профиля пользователя
        public void ListenToUserProfile(Action<UserProfile> onUpdate)
        {
            if (!CheckAuthentication()) return;
            var reference = _database.Child("users").Child(_userId).Child("profile");
            SubscribeToData(reference, onUpdate);
        }

        // Прослушивание истории эмоций пользователя
        public void ListenToUserEmotions(Action<Dictionary<string, EmotionData>> onUpdate)
        {
            if (!CheckAuthentication()) return;
            var reference = _database.Child("users").Child(_userId).Child("emotions");
            SubscribeToData(reference, onUpdate);
        }

        // --- Методы для работы с эмоциями и баночками ---

        // Добавление новой записи об эмоции
        public async Task AddEmotion(EmotionData emotion)
        {
            if (!CheckAuthentication()) return;
            if (emotion == null) throw new ArgumentNullException(nameof(emotion));

            try
            {
                // Генерируем ID, если он не задан
                if (string.IsNullOrEmpty(emotion.Id))
                {
                    // Firebase может сам генерировать ключи через Push(), но если нужен Guid:
                    emotion.Id = Guid.NewGuid().ToString();
                    // Важно: Если используешь Push() для генерации ключа Firebase,
                    // то ID нужно будет получать из результата Push() и сохранять внутри объекта уже после.
                    // Пока оставляем Guid.NewGuid().
                }

                // Сериализуем объект в JSON с помощью Newtonsoft.Json
                string jsonPayload = JsonConvert.SerializeObject(emotion, Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }); // Игнорируем null поля

                // Используем ID эмоции как ключ и сохраняем JSON-строку
                await _database.Child("users").Child(_userId).Child("emotions").Child(emotion.Id).SetRawJsonValueAsync(jsonPayload);

                MyLogger.Log($"✅ Эмоция {emotion.Type} (ID: {emotion.Id}, MyLogger.LogCategory.Firebase) добавлена для пользователя {_userId}");
            }
            catch (JsonException jsonEx) // Ловим ошибки сериализации
            {
                MyLogger.LogError($"❌ Ошибка сериализации EmotionData для пользователя {_userId}: {jsonEx.Message}\n{jsonEx.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка добавления эмоции для пользователя {_userId}: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }


        // Обновление текущей эмоции пользователя
        public async Task UpdateCurrentEmotion(string type, float intensity)
        {
            if (!CheckAuthentication()) return;

            try
            {
                var updates = new Dictionary<string, object>
                {
                    ["type"] = type,
                    ["intensity"] = intensity,
                    ["timestamp"] = ServerValue.Timestamp // Время обновления на сервере
                };

                // Обновляем узел currentEmotion целиком
                await _database.Child("users").Child(_userId).Child("currentEmotion").UpdateChildrenAsync(updates);
                MyLogger.Log($"✅ Текущая эмоция пользователя {_userId} обновлена на {type} ({intensity}, MyLogger.LogCategory.Firebase)");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления текущей эмоции для {_userId}: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        // Обновление количества в баночке с использованием транзакции
        public async Task UpdateJarAmount(string emotionType, int amountToAdd)
        {
            if (!CheckAuthentication()) return;

            if (string.IsNullOrEmpty(emotionType))
            {
                MyLogger.LogError("❌ Тип эмоции не может быть пустым для UpdateJarAmount", MyLogger.LogCategory.Firebase);
                return;
            }

            if (amountToAdd == 0) return;

            var jarRef = _database.Child("users").Child(_userId).Child("jars").Child(emotionType.ToLower());

            try
            {
                await jarRef.RunTransaction(mutableData =>
                {
                    if (mutableData.Value == null)
                    {
                        MyLogger.LogWarning($"⚠️ Узел баночки '{emotionType}' не найден. Прерываем транзакцию.", MyLogger.LogCategory.Firebase);
                        return TransactionResult.Abort();
                    }

                    try
                    {
                        var jarJson = JsonConvert.SerializeObject(mutableData.Value);
                        var jar = JsonConvert.DeserializeObject<JarData>(jarJson);

                        if (jar == null)
                        {
                            MyLogger.LogError($"❌ Не удалось десериализовать баночку '{emotionType}'", MyLogger.LogCategory.Firebase);
                            return TransactionResult.Abort();
                        }

                        int newAmount = Mathf.Clamp(jar.CurrentAmount + amountToAdd, 0, jar.Capacity);

                        if (newAmount != jar.CurrentAmount)
                        {
                            mutableData.Child("currentAmount").Value = newAmount;
                            MyLogger.Log($"🔄 {emotionType}: {jar.CurrentAmount} ➡ {newAmount}", MyLogger.LogCategory.Firebase);
                            return TransactionResult.Success(mutableData);
                        }
                        else
                        {
                            MyLogger.Log($"ℹ️ {emotionType}: значение не изменилось ({jar.CurrentAmount}, MyLogger.LogCategory.Firebase)");
                            return TransactionResult.Abort();
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ Ошибка в транзакции {emotionType}: {ex.Message}", MyLogger.LogCategory.Firebase);
                        return TransactionResult.Abort();
                    }
                });

                MyLogger.Log($"✅ Транзакция для баночки '{emotionType}' завершена.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка транзакции баночки '{emotionType}': {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }


        // Добавление очков пользователю с использованием транзакции
        public async Task AddPointsToProfile(int pointsToAdd)
        {
            if (!CheckAuthentication()) return;

            if (pointsToAdd <= 0)
            {
                MyLogger.LogWarning("⚠️ Попытка добавить 0 или отрицательное количество очков.", MyLogger.LogCategory.Firebase);
                return;
            }

            var pointsRef = _database.Child("users").Child(_userId).Child("profile").Child("totalPoints");

            try
            {
                await pointsRef.RunTransaction(mutableData =>
                {
                    long currentPoints = 0;

                    if (mutableData.Value != null && long.TryParse(mutableData.Value.ToString(), out long parsedPoints))
                    {
                        currentPoints = parsedPoints;
                    }

                    long newTotal = currentPoints + pointsToAdd;
                    mutableData.Value = newTotal;

                    MyLogger.Log($"🔄 Очки: {currentPoints} ➡ {newTotal}", MyLogger.LogCategory.Firebase);
                    return TransactionResult.Success(mutableData);
                });

                MyLogger.Log($"✅ Пользователю {_userId} начислено {pointsToAdd} очков.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка транзакции начисления очков: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        // Получает банки пользователя
        public async Task<Dictionary<string, JarData>> GetUserJars()
        {
            try
            {
                string userId = _userId;
                if (string.IsNullOrEmpty(userId))
                {
                    MyLogger.LogError("Пользователь не авторизован", MyLogger.LogCategory.Firebase);
                    return null;
                }

                var snapshot = await _database.Child("users").Child(userId).Child("jars").GetValueAsync();

                if (!snapshot.Exists)
                {
                    MyLogger.Log("Банки пользователя не найдены, создаём их", MyLogger.LogCategory.Firebase);
                    return await CreateDefaultJars();
                }

                var jarData = new Dictionary<string, JarData>();
                foreach (var child in snapshot.Children)
                {
                    // Парсим данные каждой банки
                    var jar = JsonConvert.DeserializeObject<JarData>(child.GetRawJsonValue());
                    if (jar != null)
                    {
                        jarData[child.Key] = jar;
                    }
                }

                return jarData;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при получении банок пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        // Создает банки по умолчанию для нового пользователя
        private async Task<Dictionary<string, JarData>> CreateDefaultJars()
        {
            try
            {
                string userId = _userId;
                if (string.IsNullOrEmpty(userId))
                {
                    MyLogger.LogError("Пользователь не авторизован", MyLogger.LogCategory.Firebase);
                    return null;
                }

                var jarData = new Dictionary<string, JarData>();
                
                // Создаем банку для каждого типа эмоций
                foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
                {
                    var jar = new JarData
                    {
                        Type = type.ToString(),
                        Level = 1,
                        Capacity = 100,
                        CurrentAmount = 0,
                        Customization = new JarCustomization()
                    };

                    jarData[type.ToString()] = jar;
                    
                    // Сохраняем в базу данных
                    await _database.Child("users").Child(userId).Child("jars").Child(type.ToString())
                        .SetRawJsonValueAsync(JsonConvert.SerializeObject(jar));
                }

                return jarData;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при создании банок по умолчанию: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        // Освобождение ресурсов (отписка от событий)
        public void Dispose()
        {
            try
            {
                MyLogger.Log($"Disposing DatabaseService. Отписка от {_eventHandlers.Count} слушателей...", MyLogger.LogCategory.Firebase);
                // Обходим копию ключей, чтобы избежать проблем при изменении словаря во время итерации (хотя здесь это маловероятно)
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
                
                MyLogger.Log("✅ DatabaseService: все обработчики событий удалены и ресурсы освобождены.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при освобождении ресурсов DatabaseService: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }

        #region Emotion History

        /// <summary>
        /// Получает историю эмоций
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            MyLogger.Log($"📡 [GetEmotionHistory] Начало запроса истории эмоций. UserId={_userId}, limit={limit}", MyLogger.LogCategory.Firebase);
            
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("❌ [GetEmotionHistory] Пользователь не авторизован для получения истории эмоций", MyLogger.LogCategory.Firebase);
                return new List<EmotionHistoryRecord>();
            }

            try
            {
                string path = $"users/{_userId}/emotionHistory";
                MyLogger.Log($"🔍 [GetEmotionHistory] Запрашиваем данные по пути: {path}", MyLogger.LogCategory.Firebase);
                
                Query query = _database.Child("users").Child(_userId).Child("emotionHistory").OrderByKey();
                
                // Добавляем фильтр по дате начала
                if (startDate.HasValue)
                {
                    var startTimestamp = startDate.Value.ToFileTimeUtc();
                    query = query.StartAt(null, startTimestamp.ToString());
                    MyLogger.Log($"📅 [GetEmotionHistory] Фильтр по дате начала: {startDate.Value:O}", MyLogger.LogCategory.Firebase);
                }
                
                // Добавляем фильтр по дате окончания
                if (endDate.HasValue)
                {
                    var endTimestamp = endDate.Value.ToFileTimeUtc();
                    query = query.EndAt(null, endTimestamp.ToString());
                    MyLogger.Log($"📅 [GetEmotionHistory] Фильтр по дате окончания: {endDate.Value:O}", MyLogger.LogCategory.Firebase);
                }
                
                // Ограничиваем количество записей
                query = query.LimitToLast(limit);
                
                MyLogger.Log($"⏳ [GetEmotionHistory] Выполняем запрос к Firebase...", MyLogger.LogCategory.Firebase);
                var snapshot = await query.GetValueAsync();
                
                MyLogger.Log($"📊 [GetEmotionHistory] Ответ от Firebase: Exists={snapshot.Exists}, ChildrenCount={snapshot.ChildrenCount}", MyLogger.LogCategory.Firebase);
                
                var result = new List<EmotionHistoryRecord>();
                
                if (snapshot.Exists && snapshot.ChildrenCount > 0)
                {
                    MyLogger.Log($"📋 [GetEmotionHistory] Обрабатываем {snapshot.ChildrenCount} записей...", MyLogger.LogCategory.Firebase);
                    
                    int processedCount = 0;
                    foreach (var child in snapshot.Children)
                    {
                        try
                        {
                            string rawJson = child.GetRawJsonValue();
                            MyLogger.Log($"📄 [GetEmotionHistory] Запись {processedCount + 1}: Key={child.Key}, JSON={rawJson}", MyLogger.LogCategory.Firebase);
                            
                            var record = JsonConvert.DeserializeObject<EmotionHistoryRecord>(rawJson);
                            if (record != null)
                            {
                                result.Add(record);
                                MyLogger.Log($"✅ [GetEmotionHistory] Запись {processedCount + 1} успешно десериализована: Id={record.Id}, Type={record.Type}", MyLogger.LogCategory.Firebase);
                            }
                            else
                            {
                                MyLogger.LogWarning($"⚠️ [GetEmotionHistory] Запись {processedCount + 1} десериализована как NULL", MyLogger.LogCategory.Firebase);
                            }
                            processedCount++;
                        }
                        catch (Exception ex)
                        {
                            MyLogger.LogError($"❌ [GetEmotionHistory] Ошибка при десериализации записи {processedCount + 1}: {ex.Message}", MyLogger.LogCategory.Firebase);
                        }
                    }
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ [GetEmotionHistory] Firebase вернул пустой результат или snapshot не существует", MyLogger.LogCategory.Firebase);
                }
                
                MyLogger.Log($"🎯 [GetEmotionHistory] Итого получено {result.Count} записей истории эмоций", MyLogger.LogCategory.Firebase);
                return result;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [GetEmotionHistory] Ошибка получения истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [GetEmotionHistory] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Получает историю эмоций по типу
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetEmotionHistoryByType(string emotionType, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            try
            {
                var result = new List<EmotionHistoryRecord>();
                var allRecords = await GetEmotionHistory(startDate, endDate, limit * 2);
                
                // Фильтруем по типу
                var filteredRecords = allRecords.Where(r => r.Type == emotionType).Take(limit).ToList();
                
                MyLogger.Log($"Получено {filteredRecords.Count} записей истории эмоций типа {emotionType}", MyLogger.LogCategory.Firebase);
                return filteredRecords;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения истории эмоций по типу: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Добавляет запись в историю эмоций
        /// </summary>
        public async Task AddEmotionHistoryRecord(EmotionHistoryRecord record)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для добавления записи в историю эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (record == null)
                {
                    throw new ArgumentNullException(nameof(record), "Запись не может быть null");
                }
                
                // Генерируем ID, если его нет
                if (string.IsNullOrEmpty(record.Id))
                {
                    record.Id = Guid.NewGuid().ToString();
                }
                
                var dictionary = record.ToDictionary();
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotionHistory/{record.Id}";
                _batchManager.AddUpdateOperation(path, dictionary);
                
                // Выполняем батч немедленно, так как это одиночная операция
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Запись добавлена в историю эмоций через механизм батчинга: {record.Id}, тип: {record.Type}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка добавления записи в историю эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Добавляет запись в историю эмоций на основе эмоции и события
        /// </summary>
        public async Task AddEmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для добавления записи в историю эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (emotion == null)
                {
                    throw new ArgumentNullException(nameof(emotion), "Эмоция не может быть null");
                }
                
                // Создаем запись
                var record = new EmotionHistoryRecord(emotion, eventType);
                
                // Добавляем запись
                await AddEmotionHistoryRecord(record);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка добавления записи в историю эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Пакетное добавление записей в историю
        /// </summary>
        public async Task AddEmotionHistoryBatch(List<EmotionHistoryRecord> records)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для добавления записей в историю эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (records == null || records.Count == 0)
                {
                    return;
                }
                
                // Используем механизм батчинга для пакетной обработки
                foreach (var record in records)
                {
                    // Генерируем ID, если его нет
                    if (string.IsNullOrEmpty(record.Id))
                    {
                        record.Id = Guid.NewGuid().ToString();
                    }
                    
                    // Добавляем операцию в батч
                    string path = $"users/{_userId}/emotionHistory/{record.Id}";
                    _batchManager.AddUpdateOperation(path, record.ToDictionary());
                }
                
                // Принудительно выполняем батч
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Добавлено {records.Count} записей в историю эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка пакетного добавления записей в историю эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Получает несинхронизированные записи
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionRecords(int limit = 100)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения несинхронизированных записей", MyLogger.LogCategory.Firebase);
                return new List<EmotionHistoryRecord>();
            }

            try
            {
                var query = _database.Child("users").Child(_userId).Child("emotionHistory")
                    .OrderByChild("syncStatus")
                    .EqualTo(SyncStatus.NotSynced.ToString())
                    .LimitToFirst(limit);
                
                var snapshot = await query.GetValueAsync();
                var result = new List<EmotionHistoryRecord>();
                
                if (snapshot.Exists && snapshot.ChildrenCount > 0)
                {
                    foreach (var child in snapshot.Children)
                    {
                        try
                        {
                            var record = JsonConvert.DeserializeObject<EmotionHistoryRecord>(child.GetRawJsonValue());
                            if (record != null)
                            {
                                result.Add(record);
                            }
                        }
                        catch (Exception ex)
                        {
                            MyLogger.LogError($"Ошибка при десериализации несинхронизированной записи: {ex.Message}", MyLogger.LogCategory.Firebase);
                        }
                    }
                }
                
                MyLogger.Log($"Получено {result.Count} несинхронизированных записей", MyLogger.LogCategory.Firebase);
                return result;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения несинхронизированных записей: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Обновляет статус синхронизации записи
        /// </summary>
        public async Task UpdateEmotionSyncStatus(string recordId, SyncStatus status)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления статуса синхронизации", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(recordId))
                {
                    throw new ArgumentException("ID записи не может быть пустым", nameof(recordId));
                }
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotionHistory/{recordId}/syncStatus";
                _batchManager.AddUpdateOperation(path, status.ToString());
                
                // Выполняем батч немедленно, так как это одиночная операция
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Статус синхронизации записи {recordId} обновлен на {status} через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления статуса синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Удаляет запись из истории
        /// </summary>
        public async Task DeleteEmotionHistoryRecord(string recordId)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для удаления записи", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(recordId))
                {
                    throw new ArgumentException("ID записи не может быть пустым", nameof(recordId));
                }
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotionHistory/{recordId}";
                _batchManager.AddDeleteOperation(path);
                
                // Выполняем батч немедленно
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Запись {recordId} удалена из истории эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка удаления записи из истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Получает статистику по эмоциям за период
        /// </summary>
        public async Task<Dictionary<string, int>> GetEmotionStatistics(DateTime startDate, DateTime endDate)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения статистики эмоций", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, int>();
            }

            try
            {
                var records = await GetEmotionHistory(startDate, endDate, 1000);
                var stats = new Dictionary<string, int>();
                
                foreach (var record in records)
                {
                    if (!string.IsNullOrEmpty(record.Type))
                    {
                        if (stats.ContainsKey(record.Type))
                        {
                            stats[record.Type]++;
                        }
                        else
                        {
                            stats[record.Type] = 1;
                        }
                    }
                }
                
                MyLogger.Log($"Получена статистика эмоций с {startDate} по {endDate}: {stats.Count} типов", MyLogger.LogCategory.Firebase);
                return stats;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения статистики эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        #endregion

        #region Sync Settings

        /// <summary>
        /// Получает настройки синхронизации
        /// </summary>
        public async Task<EmotionSyncSettings> GetSyncSettings()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения настроек синхронизации", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(_userId).Child("syncSettings").GetValueAsync();
                
                if (snapshot.Exists)
                {
                    var settings = JsonConvert.DeserializeObject<EmotionSyncSettings>(snapshot.GetRawJsonValue());
                    MyLogger.Log("Настройки синхронизации получены с сервера", MyLogger.LogCategory.Firebase);
                    return settings;
                }
                
                // Если настроек нет, создаем и сохраняем дефолтные
                var defaultSettings = new EmotionSyncSettings();
                await UpdateSyncSettings(defaultSettings);
                
                MyLogger.Log("Созданы и сохранены дефолтные настройки синхронизации", MyLogger.LogCategory.Firebase);
                return defaultSettings;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Обновляет настройки синхронизации
        /// </summary>
        public async Task UpdateSyncSettings(EmotionSyncSettings settings)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления настроек синхронизации", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (settings == null)
                {
                    throw new ArgumentNullException(nameof(settings), "Настройки не могут быть null");
                }
                
                // Сериализуем настройки в словарь
                var json = JsonConvert.SerializeObject(settings);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                // Сохраняем настройки в Firebase
                await _database.Child("users").Child(_userId).Child("syncSettings").UpdateChildrenAsync(dictionary);
                
                MyLogger.Log("Настройки синхронизации обновлены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        #endregion

        #region Backup

        /// <summary>
        /// Создает резервную копию данных пользователя
        /// </summary>
        public async Task<string> CreateBackup()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для создания резервной копии", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                // Получаем все данные пользователя
                var snapshot = await _database.Child("users").Child(_userId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    throw new InvalidOperationException("Данные пользователя не найдены");
                }
                
                // Создаем ID для резервной копии
                string backupId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                
                // Сохраняем резервную копию
                await _database.Child("backups").Child(_userId).Child(backupId).SetRawJsonValueAsync(snapshot.GetRawJsonValue());
                
                MyLogger.Log($"Резервная копия создана: {backupId}", MyLogger.LogCategory.Firebase);
                return backupId;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка создания резервной копии: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Восстанавливает данные из резервной копии
        /// </summary>
        public async Task<bool> RestoreFromBackup(string backupId)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для восстановления из резервной копии", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(backupId))
                {
                    throw new ArgumentException("ID резервной копии не может быть пустым", nameof(backupId));
                }
                
                // Получаем резервную копию
                var snapshot = await _database.Child("backups").Child(_userId).Child(backupId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    throw new InvalidOperationException($"Резервная копия {backupId} не найдена");
                }
                
                // Восстанавливаем данные (кроме profile, чтобы не перезаписать текущие данные авторизации)
                var backupData = JsonConvert.DeserializeObject<Dictionary<string, object>>(snapshot.GetRawJsonValue());
                
                // Фильтруем поля, которые не нужно восстанавливать
                if (backupData.ContainsKey("profile"))
                {
                    backupData.Remove("profile");
                }
                
                // Восстанавливаем остальные данные
                await _database.Child("users").Child(_userId).UpdateChildrenAsync(backupData);
                
                MyLogger.Log($"Данные восстановлены из резервной копии {backupId}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка восстановления из резервной копии: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        
        /// <summary>
        /// Получает список доступных резервных копий
        /// </summary>
        public async Task<string[]> GetAvailableBackups()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения списка резервных копий", MyLogger.LogCategory.Firebase);
                return Array.Empty<string>();
            }

            try
            {
                var snapshot = await _database.Child("backups").Child(_userId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    MyLogger.Log("Резервные копии не найдены", MyLogger.LogCategory.Firebase);
                    return Array.Empty<string>();
                }
                
                List<string> backupIds = new List<string>();
                
                foreach (var child in snapshot.Children)
                {
                    backupIds.Add(child.Key);
                }
                
                MyLogger.Log($"Найдено {backupIds.Count} резервных копий", MyLogger.LogCategory.Firebase);
                return backupIds.OrderByDescending(id => id).ToArray(); // Сортируем по убыванию даты
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения списка резервных копий: {ex.Message}", MyLogger.LogCategory.Firebase);
                return Array.Empty<string>();
            }
        }
        
        /// <summary>
        /// Проверяет подключение к базе данных
        /// </summary>
        public async Task<bool> CheckConnection()
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

        #endregion

        #region EmotionDatabaseService Implementation

        /// <summary>
        /// Получает текущие эмоции пользователя
        /// </summary>
        public async Task<Dictionary<string, EmotionData>> GetUserEmotions()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения эмоций", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, EmotionData>();
            }

            try
            {
                var snapshot = await _database.Child("users").Child(_userId).Child("emotions").GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    MyLogger.Log("Эмоции пользователя не найдены", MyLogger.LogCategory.Firebase);
                    return new Dictionary<string, EmotionData>();
                }
                
                var emotionsDict = new Dictionary<string, EmotionData>();
                
                foreach (var child in snapshot.Children)
                {
                    try
                    {
                        var emotion = JsonConvert.DeserializeObject<EmotionData>(child.GetRawJsonValue());
                        if (emotion != null)
                        {
                            emotionsDict[child.Key] = emotion;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"Ошибка десериализации эмоции: {ex.Message}", MyLogger.LogCategory.Firebase);
                    }
                }
                
                MyLogger.Log($"Получено {emotionsDict.Count} эмоций", MyLogger.LogCategory.Firebase);
                return emotionsDict;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, EmotionData>();
            }
        }
        
        /// <summary>
        /// Обновляет эмоции пользователя
        /// </summary>
        public async Task UpdateUserEmotions(Dictionary<string, EmotionData> emotions)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (emotions == null || emotions.Count == 0)
                {
                    MyLogger.LogWarning("Пустой словарь эмоций", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Используем механизм батчинга для пакетной обработки
                foreach (var kvp in emotions)
                {
                    string path = $"users/{_userId}/emotions/{kvp.Key}";
                    string json = JsonConvert.SerializeObject(kvp.Value);
                    var emotionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    _batchManager.AddUpdateOperation(path, emotionDict);
                }
                
                // Принудительно выполняем батч
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Обновлено {emotions.Count} эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет конкретную эмоцию пользователя
        /// </summary>
        public async Task UpdateUserEmotion(EmotionData emotion)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления эмоции", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (emotion == null)
                {
                    throw new ArgumentNullException(nameof(emotion));
                }
                
                if (string.IsNullOrEmpty(emotion.Id))
                {
                    emotion.Id = Guid.NewGuid().ToString();
                }
                
                string json = JsonConvert.SerializeObject(emotion);
                var emotionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotions/{emotion.Id}";
                _batchManager.AddUpdateOperation(path, emotionDict);
                
                // Выполняем батч немедленно, так как это одиночная операция
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Эмоция {emotion.Type} обновлена через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления эмоции: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Получает несинхронизированные записи истории эмоций
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionHistory(int limit = 50)
        {
            // Переиспользуем существующий метод
            return await GetUnsyncedEmotionRecords(limit);
        }
        
        /// <summary>
        /// Обновляет статус синхронизации записи истории эмоций
        /// </summary>
        public async Task UpdateEmotionHistoryRecordStatus(string recordId, SyncStatus status)
        {
            // Переиспользуем существующий метод
            await UpdateEmotionSyncStatus(recordId, status);
        }
        
        #endregion
        
        #region UserProfileDatabaseService Implementation
        
        /// <summary>
        /// Создает профиль пользователя
        /// </summary>
        public async Task CreateUserProfile(UserProfile profile, string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для создания профиля", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(profile);
                
                await _database.Child("users").Child(targetUserId).Child("profile")
                    .SetRawJsonValueAsync(json);
                
                MyLogger.Log($"Профиль пользователя {targetUserId} создан", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка создания профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет профиль пользователя
        /// </summary>
        public async Task UpdateUserProfile(UserProfile profile, string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для обновления профиля", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(profile);
                var updates = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                await _database.Child("users").Child(targetUserId).Child("profile")
                    .UpdateChildrenAsync(updates);
                
                MyLogger.Log($"Профиль пользователя {targetUserId} обновлен", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет поле профиля пользователя
        /// </summary>
        public async Task UpdateUserProfileField(string field, object value, string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для обновления поля профиля", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(field))
                {
                    throw new ArgumentException("Поле не может быть пустым", nameof(field));
                }
                
                await _database.Child("users").Child(targetUserId).Child("profile").Child(field)
                    .SetValueAsync(value);
                
                MyLogger.Log($"Поле {field} профиля пользователя {targetUserId} обновлено", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления поля профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Проверяет существование профиля пользователя
        /// </summary>
        public async Task<bool> UserProfileExists(string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для проверки существования профиля", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(targetUserId).Child("profile").GetValueAsync();
                return snapshot.Exists;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки существования профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        
        /// <summary>
        /// Проверяет существование никнейма
        /// </summary>
        public async Task<bool> NicknameExists(string nickname)
        {
            try
            {
                if (string.IsNullOrEmpty(nickname))
                {
                    throw new ArgumentException("Никнейм не может быть пустым", nameof(nickname));
                }
                
                var query = _database.Child("users").OrderByChild("profile/nickname").EqualTo(nickname).LimitToFirst(1);
                var snapshot = await query.GetValueAsync();
                
                return snapshot.Exists && snapshot.ChildrenCount > 0;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки существования никнейма: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        
        /// <summary>
        /// Проверяет доступность никнейма
        /// </summary>
        public async Task<(bool available, string error)> CheckNicknameAvailability(string nickname)
        {
            try
            {
                if (string.IsNullOrEmpty(nickname))
                {
                    return (false, "Никнейм не может быть пустым");
                }
                
                if (nickname.Length < 3)
                {
                    return (false, "Никнейм должен содержать не менее 3 символов");
                }
                
                if (nickname.Length > 20)
                {
                    return (false, "Никнейм не должен превышать 20 символов");
                }
                
                if (!System.Text.RegularExpressions.Regex.IsMatch(nickname, "^[a-zA-Z0-9_]+$"))
                {
                    return (false, "Никнейм может содержать только латинские буквы, цифры и символ подчеркивания");
                }
                
                bool exists = await NicknameExists(nickname);
                
                return exists ? 
                    (false, "Этот никнейм уже занят") : 
                    (true, null);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки доступности никнейма: {ex.Message}", MyLogger.LogCategory.Firebase);
                return (false, "Произошла ошибка при проверке никнейма");
            }
        }
        
        #endregion

        /// <summary>
        /// Обновляет статусы синхронизации нескольких записей одним батчем
        /// </summary>
        public async Task UpdateEmotionSyncStatusBatch(Dictionary<string, SyncStatus> recordStatusPairs)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления статусов синхронизации", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (recordStatusPairs == null || recordStatusPairs.Count == 0)
                {
                    MyLogger.LogWarning("Пустой словарь записей для обновления статусов", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Используем механизм батчинга для всех записей
                foreach (var kvp in recordStatusPairs)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                    {
                        MyLogger.LogWarning("Пропуск записи с пустым ID", MyLogger.LogCategory.Firebase);
                        continue;
                    }
                    
                    string path = $"users/{_userId}/emotionHistory/{kvp.Key}/syncStatus";
                    _batchManager.AddUpdateOperation(path, kvp.Value.ToString());
                }
                
                // Выполняем батч для всех операций
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Обновлены статусы синхронизации для {recordStatusPairs.Count} записей через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка пакетного обновления статусов синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Удаляет несколько записей из истории одним батчем
        /// </summary>
        public async Task DeleteEmotionHistoryRecordBatch(List<string> recordIds)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для удаления записей", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (recordIds == null || recordIds.Count == 0)
                {
                    MyLogger.LogWarning("Пустой список записей для удаления", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Используем механизм батчинга для всех записей
                foreach (var recordId in recordIds)
                {
                    if (string.IsNullOrEmpty(recordId))
                    {
                        MyLogger.LogWarning("Пропуск записи с пустым ID", MyLogger.LogCategory.Firebase);
                        continue;
                    }
                    
                    string path = $"users/{_userId}/emotionHistory/{recordId}";
                    _batchManager.AddDeleteOperation(path);
                }
                
                // Выполняем батч для всех операций
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Удалено {recordIds.Count} записей из истории эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка пакетного удаления записей из истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Сохраняет запись истории эмоций в базе данных
        /// </summary>
        /// <param name="record">Запись для сохранения</param>
        /// <returns>True если запись сохранена успешно, иначе False</returns>
        public async Task<bool> SaveEmotionHistoryRecord(EmotionHistoryRecord record)
        {
            if (!CheckAuthentication())
                return false;

            if (record == null)
            {
                MyLogger.LogError("❌ Запись истории эмоций не может быть пустой", MyLogger.LogCategory.Firebase);
                return false;
            }
            
            // Валидация данных перед сохранением
            if (_validationService != null && _validationService.HasValidator<EmotionHistoryRecord>())
            {
                var validationResult = _validationService.Validate<EmotionHistoryRecord>(record);
                if (!validationResult.IsValid)
                {
                    validationResult.CheckAndLogErrors("EmotionHistoryRecord");
                    MyLogger.LogError("❌ Валидация записи истории эмоций не пройдена. Запись не будет сохранена.", MyLogger.LogCategory.Firebase);
                    return false;
                }
            }

            try
            {
                var userHistoryRef = _database.Child("users").Child(_userId).Child("emotionHistory").Child(record.Id);
                await userHistoryRef.SetValueAsync(record.ToDictionary());
                
                // Кэширование записи
                _emotionHistoryCache.AddOrUpdateRecord(record);
                
                MyLogger.Log($"✅ Запись истории эмоций сохранена: {record.Id}, тип: {record.Type}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения записи истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        #region GameData Management

        public async Task SaveUserGameData(GameData gameData)
        {
            if (!IsAuthenticated)
            {
                MyLogger.LogError("[DatabaseService] Невозможно сохранить GameData: пользователь не аутентифицирован.", MyLogger.LogCategory.Firebase);
                return;
            }
            if (gameData == null)
            {
                MyLogger.LogError("[DatabaseService] Невозможно сохранить GameData: передан null объект.", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(gameData, Formatting.Indented); // Formatting.Indented для читаемости в Firebase
                DatabaseReference gameDataRef = _database.Child("users").Child(_userId).Child("gameData");
                await gameDataRef.SetRawJsonValueAsync(jsonData);
                MyLogger.Log($"[DatabaseService] GameData для пользователя {_userId} успешно сохранено в Firebase.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[DatabaseService] Ошибка при сохранении GameData в Firebase: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                // Можно добавить обработку исключений, например, повторную попытку или уведомление пользователя
            }
        }

        public async Task<GameData> LoadUserGameData()
        {
            if (!IsAuthenticated)
            {
                MyLogger.LogWarning("[DatabaseService] Невозможно загрузить GameData: пользователь не аутентифицирован.", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                DatabaseReference gameDataRef = _database.Child("users").Child(_userId).Child("gameData");
                DataSnapshot snapshot = await gameDataRef.GetValueAsync();

                if (snapshot.Exists)
                {
                    string jsonData = snapshot.GetRawJsonValue();
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        GameData gameData = JsonConvert.DeserializeObject<GameData>(jsonData);
                        MyLogger.Log($"[DatabaseService] GameData для пользователя {_userId} успешно загружено из Firebase.", MyLogger.LogCategory.Firebase);
                        return gameData;
                    }
                    else
                    {
                        MyLogger.LogWarning($"[DatabaseService] GameData для пользователя {_userId} существует в Firebase, но содержит пустые данные.", MyLogger.LogCategory.Firebase);
                        return new GameData(); // Возвращаем новый экземпляр, чтобы избежать null
                    }
                }
                else
                {
                    MyLogger.Log($"[DatabaseService] GameData для пользователя {_userId} не найдено в Firebase. Будут использованы данные по умолчанию.", MyLogger.LogCategory.Firebase);
                    return new GameData(); // Возвращаем новый экземпляр, если данных нет
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[DatabaseService] Ошибка при загрузке GameData из Firebase: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
                return new GameData(); // В случае ошибки возвращаем новый экземпляр
            }
        }

        #endregion
    }
}
