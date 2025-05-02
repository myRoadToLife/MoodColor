// Assets/App/Develop/AppServices/Firebase/Database/Services/DatabaseService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Common.Cache;
using App.Develop.AppServices.Firebase.Common.Helpers;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion; // Убедись, что EmotionTypes здесь определен
using Firebase.Database;
using Newtonsoft.Json; // Используется для десериализации
using UnityEngine;
using UserProfile = App.Develop.AppServices.Firebase.Database.Models.UserProfile;

namespace App.Develop.AppServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для работы с базой данных Firebase
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        #region Private Fields
        private readonly DatabaseReference _database;
        private readonly FirebaseCacheManager _cacheManager;
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
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса базы данных
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэширования</param>
        public DatabaseService(DatabaseReference database, FirebaseCacheManager cacheManager)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            
            Debug.Log("✅ DatabaseService инициализирован");
        }
        #endregion

        // Метод для обновления ID пользователя при аутентификации
        public void UpdateUserId(string userId)
        {
            _userId = userId;
            Debug.Log($"ID пользователя в DatabaseService обновлен: {userId}");
        }

        // Проверка, аутентифицирован ли пользователь (установлен ли _userId)
        private bool CheckAuthentication()
        {
            if (string.IsNullOrEmpty(_userId))
            {
                Debug.LogWarning("⚠️ Операция требует авторизации пользователя");
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
                    Debug.LogWarning($"👤 Пользователь {email} (ID: {userId}) уже существует");
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
                Debug.Log($"✅ Профиль пользователя {email} (ID: {userId}) создан");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка создания пользователя (ID: {userId}): {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        // Получение профиля пользователя
        public async Task<UserProfile> GetUserProfile(string userId = null)
        {
            string targetUserId = userId ?? _userId; // Используем переданный ID или ID текущего пользователя

            if (string.IsNullOrEmpty(targetUserId))
            {
                Debug.LogWarning("⚠️ ID пользователя не указан для получения профиля");
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

                Debug.LogWarning($"Профиль для пользователя {targetUserId} не найден.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка получения профиля (ID: {targetUserId}): {ex.Message}\n{ex.StackTrace}");
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
                Debug.Log($"✅ Данные пользователя {_userId} обновлены.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка обновления данных пользователя {_userId}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        // --- Методы прослушивания (Listeners) ---

        // Базовый метод для подписки на события ValueChanged
        private void SubscribeToData <T>(DatabaseReference reference, Action<T> onUpdate)
        {
            if (_eventHandlers.ContainsKey(reference))
            {
                Debug.LogWarning($"Попытка повторной подписки на {reference.Key}");
                return; // Уже подписаны
            }

            _activeListeners.Add(reference);

            EventHandler<ValueChangedEventArgs> handler = (sender, args) =>
            {
                if (args.DatabaseError != null)
                {
                    Debug.LogError($"Ошибка Firebase при прослушивании {reference.Key}: {args.DatabaseError.Message}");
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
                        Debug.LogError($"❌ Ошибка обработки данных для {reference.Key}: {ex.Message}\n{ex.StackTrace}");
                        // Можно добавить логику уведомления пользователя или отписки при критической ошибке
                    }
                }
                else
                {
                    Debug.Log($"Данные для {reference.Key} не найдены или пусты.");
                    // Вызываем onUpdate с default(T), чтобы обработать случай отсутствия данных
                    onUpdate?.Invoke(default(T));
                }
            };

            _eventHandlers[reference] = handler; // Сохраняем обработчик
            reference.ValueChanged += handler; // Подписываемся
            Debug.Log($"Подписка на {reference.Key} установлена.");
        }

        // Прослушивание эмоций в регионе
        public void ListenToRegionEmotions(string regionId, Action<Dictionary<string, int>> onUpdate)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                Debug.LogWarning("⚠️ ID региона не может быть пустым для ListenToRegionEmotions");
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

                Debug.Log($"✅ Эмоция {emotion.Type} (ID: {emotion.Id}) добавлена для пользователя {_userId}");
            }
            catch (JsonException jsonEx) // Ловим ошибки сериализации
            {
                Debug.LogError($"❌ Ошибка сериализации EmotionData для пользователя {_userId}: {jsonEx.Message}\n{jsonEx.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка добавления эмоции для пользователя {_userId}: {ex.Message}\n{ex.StackTrace}");
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
                Debug.Log($"✅ Текущая эмоция пользователя {_userId} обновлена на {type} ({intensity})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка обновления текущей эмоции для {_userId}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        // Обновление количества в баночке с использованием транзакции
        public async Task UpdateJarAmount(string emotionType, int amountToAdd)
        {
            if (!CheckAuthentication()) return;

            if (string.IsNullOrEmpty(emotionType))
            {
                Debug.LogError("❌ Тип эмоции не может быть пустым для UpdateJarAmount");
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
                        Debug.LogWarning($"⚠️ Узел баночки '{emotionType}' не найден. Прерываем транзакцию.");
                        return TransactionResult.Abort();
                    }

                    try
                    {
                        var jarJson = JsonConvert.SerializeObject(mutableData.Value);
                        var jar = JsonConvert.DeserializeObject<JarData>(jarJson);

                        if (jar == null)
                        {
                            Debug.LogError($"❌ Не удалось десериализовать баночку '{emotionType}'");
                            return TransactionResult.Abort();
                        }

                        int newAmount = Mathf.Clamp(jar.CurrentAmount + amountToAdd, 0, jar.Capacity);

                        if (newAmount != jar.CurrentAmount)
                        {
                            mutableData.Child("currentAmount").Value = newAmount;
                            Debug.Log($"🔄 {emotionType}: {jar.CurrentAmount} ➡ {newAmount}");
                            return TransactionResult.Success(mutableData);
                        }
                        else
                        {
                            Debug.Log($"ℹ️ {emotionType}: значение не изменилось ({jar.CurrentAmount})");
                            return TransactionResult.Abort();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Ошибка в транзакции {emotionType}: {ex.Message}");
                        return TransactionResult.Abort();
                    }
                });

                Debug.Log($"✅ Транзакция для баночки '{emotionType}' завершена.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка транзакции баночки '{emotionType}': {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        // Добавление очков пользователю с использованием транзакции
        public async Task AddPointsToProfile(int pointsToAdd)
        {
            if (!CheckAuthentication()) return;

            if (pointsToAdd <= 0)
            {
                Debug.LogWarning("⚠️ Попытка добавить 0 или отрицательное количество очков.");
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

                    Debug.Log($"🔄 Очки: {currentPoints} ➡ {newTotal}");
                    return TransactionResult.Success(mutableData);
                });

                Debug.Log($"✅ Пользователю {_userId} начислено {pointsToAdd} очков.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка транзакции начисления очков: {ex.Message}\n{ex.StackTrace}");
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
                    Debug.LogError("Пользователь не авторизован");
                    return null;
                }

                var snapshot = await _database.Child("users").Child(userId).Child("jars").GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.Log("Банки пользователя не найдены, создаём их");
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
                Debug.LogError($"Ошибка при получении банок пользователя: {ex.Message}");
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
                    Debug.LogError("Пользователь не авторизован");
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
                Debug.LogError($"Ошибка при создании банок по умолчанию: {ex.Message}");
                throw;
            }
        }

        // Освобождение ресурсов (отписка от событий)
        public void Dispose()
        {
            try
            {
                Debug.Log($"Disposing DatabaseService. Отписка от {_eventHandlers.Count} слушателей...");
                // Обходим копию ключей, чтобы избежать проблем при изменении словаря во время итерации (хотя здесь это маловероятно)
                var referencesToUnsubscribe = new List<DatabaseReference>(_eventHandlers.Keys);

                foreach (var reference in referencesToUnsubscribe)
                {
                    if (_eventHandlers.TryGetValue(reference, out var handler))
                    {
                        reference.ValueChanged -= handler; // Отписываемся
                        Debug.Log($"Отписка от {reference.Key} выполнена.");
                    }
                }

                _eventHandlers.Clear(); // Очищаем словарь обработчиков
                _activeListeners.Clear(); // Очищаем список активных ссылок
                Debug.Log("✅ DatabaseService: все обработчики событий удалены и ресурсы освобождены.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при освобождении ресурсов DatabaseService: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #region Emotion History

        /// <summary>
        /// Получает историю эмоций
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            if (!CheckAuthentication())
            {
                Debug.LogWarning("Пользователь не авторизован для получения истории эмоций");
                return new List<EmotionHistoryRecord>();
            }

            try
            {
                Query query = _database.Child("users").Child(_userId).Child("emotionHistory").OrderByKey();
                
                // Добавляем фильтр по дате начала
                if (startDate.HasValue)
                {
                    var startTimestamp = startDate.Value.ToFileTimeUtc();
                    query = query.StartAt(null, startTimestamp.ToString());
                }
                
                // Добавляем фильтр по дате окончания
                if (endDate.HasValue)
                {
                    var endTimestamp = endDate.Value.ToFileTimeUtc();
                    query = query.EndAt(null, endTimestamp.ToString());
                }
                
                // Ограничиваем количество записей
                query = query.LimitToLast(limit);
                
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
                            Debug.LogError($"Ошибка при десериализации записи истории эмоций: {ex.Message}");
                        }
                    }
                }
                
                Debug.Log($"Получено {result.Count} записей истории эмоций");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения истории эмоций: {ex.Message}");
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
                
                Debug.Log($"Получено {filteredRecords.Count} записей истории эмоций типа {emotionType}");
                return filteredRecords;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения истории эмоций по типу: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для добавления записи в историю эмоций");
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
                
                // Сохраняем запись в Firebase
                await _database.Child("users").Child(_userId).Child("emotionHistory").Child(record.Id)
                    .SetValueAsync(dictionary);
                
                Debug.Log($"Запись добавлена в историю эмоций: {record.Id}, тип: {record.Type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка добавления записи в историю эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для добавления записи в историю эмоций");
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
                Debug.LogError($"Ошибка добавления записи в историю эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для добавления записей в историю эмоций");
                return;
            }

            try
            {
                if (records == null || records.Count == 0)
                {
                    return;
                }
                
                var updates = new Dictionary<string, object>();
                
                foreach (var record in records)
                {
                    // Генерируем ID, если его нет
                    if (string.IsNullOrEmpty(record.Id))
                    {
                        record.Id = Guid.NewGuid().ToString();
                    }
                    
                    updates[$"users/{_userId}/emotionHistory/{record.Id}"] = record.ToDictionary();
                }
                
                await _database.UpdateChildrenAsync(updates);
                
                Debug.Log($"Добавлено {records.Count} записей в историю эмоций");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка пакетного добавления записей в историю эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для получения несинхронизированных записей");
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
                            Debug.LogError($"Ошибка при десериализации несинхронизированной записи: {ex.Message}");
                        }
                    }
                }
                
                Debug.Log($"Получено {result.Count} несинхронизированных записей");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения несинхронизированных записей: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для обновления статуса синхронизации");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(recordId))
                {
                    throw new ArgumentException("ID записи не может быть пустым", nameof(recordId));
                }
                
                await _database.Child("users").Child(_userId).Child("emotionHistory").Child(recordId).Child("syncStatus")
                    .SetValueAsync(status.ToString());
                
                Debug.Log($"Статус синхронизации записи {recordId} обновлен на {status}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка обновления статуса синхронизации: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для удаления записи");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(recordId))
                {
                    throw new ArgumentException("ID записи не может быть пустым", nameof(recordId));
                }
                
                await _database.Child("users").Child(_userId).Child("emotionHistory").Child(recordId).RemoveValueAsync();
                
                Debug.Log($"Запись {recordId} удалена из истории эмоций");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка удаления записи из истории эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для получения статистики эмоций");
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
                
                Debug.Log($"Получена статистика эмоций с {startDate} по {endDate}: {stats.Count} типов");
                return stats;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения статистики эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для получения настроек синхронизации");
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(_userId).Child("syncSettings").GetValueAsync();
                
                if (snapshot.Exists)
                {
                    var settings = JsonConvert.DeserializeObject<EmotionSyncSettings>(snapshot.GetRawJsonValue());
                    Debug.Log("Настройки синхронизации получены с сервера");
                    return settings;
                }
                
                // Если настроек нет, создаем и сохраняем дефолтные
                var defaultSettings = new EmotionSyncSettings();
                await UpdateSyncSettings(defaultSettings);
                
                Debug.Log("Созданы и сохранены дефолтные настройки синхронизации");
                return defaultSettings;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения настроек синхронизации: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для обновления настроек синхронизации");
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
                
                Debug.Log("Настройки синхронизации обновлены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка обновления настроек синхронизации: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для создания резервной копии");
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
                
                Debug.Log($"Резервная копия создана: {backupId}");
                return backupId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка создания резервной копии: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для восстановления из резервной копии");
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
                
                Debug.Log($"Данные восстановлены из резервной копии {backupId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка восстановления из резервной копии: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для получения списка резервных копий");
                return Array.Empty<string>();
            }

            try
            {
                var snapshot = await _database.Child("backups").Child(_userId).GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    Debug.Log("Резервные копии не найдены");
                    return Array.Empty<string>();
                }
                
                List<string> backupIds = new List<string>();
                
                foreach (var child in snapshot.Children)
                {
                    backupIds.Add(child.Key);
                }
                
                Debug.Log($"Найдено {backupIds.Count} резервных копий");
                return backupIds.OrderByDescending(id => id).ToArray(); // Сортируем по убыванию даты
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения списка резервных копий: {ex.Message}");
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
                
                Debug.Log($"Статус подключения к Firebase: {(isConnected ? "Подключено" : "Не подключено")}");
                return isConnected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка проверки подключения: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для получения эмоций");
                return new Dictionary<string, EmotionData>();
            }

            try
            {
                var snapshot = await _database.Child("users").Child(_userId).Child("emotions").GetValueAsync();
                
                if (!snapshot.Exists)
                {
                    Debug.Log("Эмоции пользователя не найдены");
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
                        Debug.LogError($"Ошибка десериализации эмоции: {ex.Message}");
                    }
                }
                
                Debug.Log($"Получено {emotionsDict.Count} эмоций");
                return emotionsDict;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка получения эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для обновления эмоций");
                return;
            }

            try
            {
                if (emotions == null || emotions.Count == 0)
                {
                    Debug.LogWarning("Пустой словарь эмоций");
                    return;
                }
                
                var updates = new Dictionary<string, object>();
                
                foreach (var kvp in emotions)
                {
                    string json = JsonConvert.SerializeObject(kvp.Value);
                    var emotionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    updates[$"emotions/{kvp.Key}"] = emotionDict;
                }
                
                await _database.Child("users").Child(_userId).UpdateChildrenAsync(updates);
                
                Debug.Log($"Обновлено {emotions.Count} эмоций");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка обновления эмоций: {ex.Message}");
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
                Debug.LogWarning("Пользователь не авторизован для обновления эмоции");
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
                
                await _database.Child("users").Child(_userId).Child("emotions").Child(emotion.Id)
                    .SetRawJsonValueAsync(json);
                
                Debug.Log($"Эмоция {emotion.Type} обновлена");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка обновления эмоции: {ex.Message}");
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
                Debug.LogWarning("ID пользователя не указан для создания профиля");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(profile);
                
                await _database.Child("users").Child(targetUserId).Child("profile")
                    .SetRawJsonValueAsync(json);
                
                Debug.Log($"Профиль пользователя {targetUserId} создан");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка создания профиля: {ex.Message}");
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
                Debug.LogWarning("ID пользователя не указан для обновления профиля");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(profile);
                var updates = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                await _database.Child("users").Child(targetUserId).Child("profile")
                    .UpdateChildrenAsync(updates);
                
                Debug.Log($"Профиль пользователя {targetUserId} обновлен");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка обновления профиля: {ex.Message}");
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
                Debug.LogWarning("ID пользователя не указан для обновления поля профиля");
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
                
                Debug.Log($"Поле {field} профиля пользователя {targetUserId} обновлено");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка обновления поля профиля: {ex.Message}");
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
                Debug.LogWarning("ID пользователя не указан для проверки существования профиля");
                return false;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(targetUserId).Child("profile").GetValueAsync();
                return snapshot.Exists;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка проверки существования профиля: {ex.Message}");
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
                Debug.LogError($"Ошибка проверки существования никнейма: {ex.Message}");
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
                Debug.LogError($"Ошибка проверки доступности никнейма: {ex.Message}");
                return (false, "Произошла ошибка при проверке никнейма");
            }
        }
        
        #endregion
    }
}
