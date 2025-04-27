// Assets/App/Develop/AppServices/Firebase/Database/Services/DatabaseService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion; // Убедись, что EmotionTypes здесь определен
using Firebase.Database;
using Newtonsoft.Json; // Используется для десериализации
using UnityEngine;
using UserProfile = App.Develop.AppServices.Firebase.Database.Models.UserProfile;


namespace App.Develop.AppServices.Firebase.Database.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly DatabaseReference _database;
        private string _userId;
        private readonly List<DatabaseReference> _activeListeners = new List<DatabaseReference>();

        // Словарь для хранения ссылок на обработчики событий для корректной отписки
        private readonly Dictionary<DatabaseReference, EventHandler<ValueChangedEventArgs>> _eventHandlers =
            new Dictionary<DatabaseReference, EventHandler<ValueChangedEventArgs>>();

        public DatabaseService(DatabaseReference database, string userId = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _userId = userId;
        }

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
        public async Task UpdateCurrentEmotion(string type, int intensity)
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
    }
}
