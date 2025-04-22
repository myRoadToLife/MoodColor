// Assets/App/Develop/AppServices/Firebase/Database/Services/DatabaseService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.Emotion;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Database.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly DatabaseReference _database;
        private string _userId;
        private readonly List<DatabaseReference> _activeListeners = new List<DatabaseReference>();

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

        private bool CheckAuthentication()
        {
            if (string.IsNullOrEmpty(_userId))
            {
                Debug.LogWarning("⚠️ Операция требует авторизации пользователя");
                return false;
            }

            return true;
        }

        public async Task CreateNewUser(string userId, string email)
        {
            try
            {
                var userSnapshot = await _database.Child("users").Child(userId).GetValueAsync();

                if (userSnapshot.Exists)
                {
                    Debug.LogWarning($"👤 Пользователь {email} уже существует");
                    return;
                }

                var userData = new Dictionary<string, object>
                {
                    ["profile"] = new Dictionary<string, object>
                    {
                        ["email"] = email,
                        ["createdAt"] = ServerValue.Timestamp,
                        ["lastActive"] = ServerValue.Timestamp,
                        ["totalPoints"] = 0,
                        ["settings"] = new Dictionary<string, object>
                        {
                            ["notifications"] = true,
                            ["theme"] = "default",
                            ["sound"] = true
                        }
                    }
                };

                // Создаем баночки для каждого типа эмоций
                var jars = new Dictionary<string, object>();

                foreach (var emotionType in Enum.GetNames(typeof(EmotionTypes)))
                {
                    jars[emotionType.ToLower()] = new Dictionary<string, object>
                    {
                        ["type"] = emotionType,
                        ["level"] = 1,
                        ["capacity"] = 100,
                        ["currentAmount"] = 0,
                        ["customization"] = new Dictionary<string, object>
                        {
                            ["color"] = "default",
                            ["pattern"] = "default",
                            ["effects"] = new List<string>()
                        }
                    };
                }

                userData["jars"] = jars;

                await _database.Child("users").Child(userId).UpdateChildrenAsync(userData);
                Debug.Log($"✅ Профиль пользователя {email} создан");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка создания пользователя: {ex.Message}");
                throw;
            }
        }

        public async Task<UserProfile> GetUserProfile(string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                Debug.LogWarning("⚠️ ID пользователя не указан для получения профиля");
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(targetUserId).Child("profile").GetValueAsync();

                if (snapshot.Exists)
                {
                    var json = JsonConvert.SerializeObject(snapshot.Value);
                    return JsonConvert.DeserializeObject<UserProfile>(json);
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка получения профиля: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserData(Dictionary<string, object> updates)
        {
            if (!CheckAuthentication())
            {
                throw new InvalidOperationException("Пользователь не авторизован");
            }

            try
            {
                await _database.Child("users").Child(_userId).UpdateChildrenAsync(updates);
                Debug.Log("✅ Данные пользователя обновлены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка обновления данных пользователя: {ex.Message}");
                throw;
            }
        }

        public void ListenToRegionEmotions(string regionId, Action<Dictionary<string, int>> onUpdate)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                Debug.LogWarning("⚠️ ID региона не может быть пустым");
                return;
            }

            var reference = _database.Child("regions").Child(regionId).Child("emotions");
            _activeListeners.Add(reference);

            // Создаем обработчик события
            EventHandler<ValueChangedEventArgs> handler = (sender, args) =>
            {
                if (args?.Snapshot == null || !args.Snapshot.Exists) return;

                try
                {
                    var emotions = new Dictionary<string, int>();

                    foreach (var child in args.Snapshot.Children)
                    {
                        emotions[child.Key] = Convert.ToInt32(child.Value);
                    }

                    onUpdate?.Invoke(emotions);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка обработки данных эмоций: {ex}");
                }
            };

            // Сохраняем обработчик для последующего удаления
            _eventHandlers[reference] = handler;

            // Подписываемся на событие
            reference.ValueChanged += handler;
        }

        public void ListenToJars(Action<Dictionary<string, JarData>> onUpdate)
        {
            if (!CheckAuthentication()) return;

            var reference = _database.Child("users").Child(_userId).Child("jars");
            _activeListeners.Add(reference);

            // Создаем обработчик события
            EventHandler<ValueChangedEventArgs> handler = (sender, args) =>
            {
                if (args?.Snapshot == null || !args.Snapshot.Exists) return;

                try
                {
                    var json = JsonConvert.SerializeObject(args.Snapshot.Value);
                    var jars = JsonConvert.DeserializeObject<Dictionary<string, JarData>>(json);
                    onUpdate?.Invoke(jars);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка обработки данных банок: {ex}");
                }
            };

            // Сохраняем обработчик для последующего удаления
            _eventHandlers[reference] = handler;

            // Подписываемся на событие
            reference.ValueChanged += handler;
        }

        public void ListenToUserProfile(Action<UserProfile> onUpdate)
        {
            if (!CheckAuthentication()) return;

            var reference = _database.Child("users").Child(_userId).Child("profile");
            _activeListeners.Add(reference);

            // Создаем обработчик события
            EventHandler<ValueChangedEventArgs> handler = (sender, args) =>
            {
                if (args?.Snapshot == null || !args.Snapshot.Exists) return;

                try
                {
                    var json = JsonConvert.SerializeObject(args.Snapshot.Value);
                    var profile = JsonConvert.DeserializeObject<UserProfile>(json);
                    onUpdate?.Invoke(profile);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка обработки данных профиля: {ex}");
                }
            };

            // Сохраняем обработчик для последующего удаления
            _eventHandlers[reference] = handler;

            // Подписываемся на событие
            reference.ValueChanged += handler;
        }

        public void ListenToUserEmotions(Action<Dictionary<string, EmotionData>> onUpdate)
        {
            if (!CheckAuthentication()) return;

            var reference = _database.Child("users").Child(_userId).Child("emotions");
            _activeListeners.Add(reference);

            // Создаем обработчик события
            EventHandler<ValueChangedEventArgs> handler = (sender, args) =>
            {
                if (args?.Snapshot == null || !args.Snapshot.Exists) return;

                try
                {
                    var json = JsonConvert.SerializeObject(args.Snapshot.Value);
                    var emotions = JsonConvert.DeserializeObject<Dictionary<string, EmotionData>>(json);
                    onUpdate?.Invoke(emotions);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка обработки данных эмоций пользователя: {ex}");
                }
            };

            // Сохраняем обработчик для последующего удаления
            _eventHandlers[reference] = handler;

            // Подписываемся на событие
            reference.ValueChanged += handler;
        }

        public async Task AddEmotion(EmotionData emotion)
        {
            if (!CheckAuthentication()) return;

            try
            {
                var emotionId = Guid.NewGuid().ToString();
                await _database.Child("users").Child(_userId).Child("emotions").Child(emotionId).SetValueAsync(emotion);
                Debug.Log($"✅ Эмоция {emotion.Type} добавлена");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка добавления эмоции: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateCurrentEmotion(string type, int intensity)
        {
            if (!CheckAuthentication()) return;

            try
            {
                var updates = new Dictionary<string, object>
                {
                    ["currentEmotion/type"] = type,
                    ["currentEmotion/intensity"] = intensity,
                    ["currentEmotion/timestamp"] = ServerValue.Timestamp
                };

                await _database.Child("users").Child(_userId).UpdateChildrenAsync(updates);
                Debug.Log($"✅ Текущая эмоция обновлена на {type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка обновления текущей эмоции: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                // Обходим словарь и отписываем каждый обработчик
                foreach (var pair in _eventHandlers)
                {
                    var reference = pair.Key;
                    var handler = pair.Value;

                    // Правильно отписываемся от события
                    reference.ValueChanged -= handler;
                }

                _eventHandlers.Clear();
                _activeListeners.Clear();
                Debug.Log("✅ DatabaseService: все обработчики событий удалены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при освобождении ресурсов DatabaseService: {ex.Message}");
            }
        }
    }
}
