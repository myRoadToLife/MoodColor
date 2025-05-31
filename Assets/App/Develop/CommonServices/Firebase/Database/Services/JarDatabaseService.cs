using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для работы с баночками эмоций в Firebase Database
    /// </summary>
    public class JarDatabaseService : FirebaseDatabaseServiceBase, IJarDatabaseService
    {
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса баночек эмоций
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public JarDatabaseService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null) 
            : base(database, cacheManager, validationService)
        {
            MyLogger.Log("✅ JarDatabaseService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region IJarDatabaseService Implementation
        /// <summary>
        /// Получает все баночки пользователя
        /// </summary>
        public async Task<Dictionary<string, JarData>> GetUserJars()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogError("Пользователь не авторизован", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(_userId).Child("jars").GetValueAsync();

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

        /// <summary>
        /// Обновляет баночку эмоций
        /// </summary>
        public async Task UpdateJar(string emotionType, JarData jar)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления баночки", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(emotionType))
                {
                    throw new ArgumentException("Тип эмоции не может быть пустым", nameof(emotionType));
                }

                if (jar == null)
                {
                    throw new ArgumentNullException(nameof(jar), "Данные баночки не могут быть null");
                }

                string json = JsonConvert.SerializeObject(jar);
                var jarRef = _database.Child("users").Child(_userId).Child("jars").Child(emotionType.ToLower());
                await jarRef.SetRawJsonValueAsync(json);

                MyLogger.Log($"Баночка для типа эмоции {emotionType} успешно обновлена", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении баночки {emotionType}: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Обновляет количество эмоций в баночке
        /// </summary>
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
                            MyLogger.Log($"ℹ️ {emotionType}: значение не изменилось ({jar.CurrentAmount})", MyLogger.LogCategory.Firebase);
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

        /// <summary>
        /// Обновляет уровень баночки
        /// </summary>
        public async Task UpdateJarLevel(string emotionType, int level)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления уровня баночки", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(emotionType))
                {
                    throw new ArgumentException("Тип эмоции не может быть пустым", nameof(emotionType));
                }

                if (level <= 0)
                {
                    throw new ArgumentException("Уровень должен быть положительным числом", nameof(level));
                }

                // Обновляем только поле level
                var jarRef = _database.Child("users").Child(_userId).Child("jars").Child(emotionType.ToLower()).Child("level");
                await jarRef.SetValueAsync(level);

                // Обновляем емкость баночки в зависимости от уровня
                int newCapacity = CalculateCapacityForLevel(level);
                var capacityRef = _database.Child("users").Child(_userId).Child("jars").Child(emotionType.ToLower()).Child("capacity");
                await capacityRef.SetValueAsync(newCapacity);

                MyLogger.Log($"Уровень баночки {emotionType} обновлен до {level}, новая емкость: {newCapacity}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении уровня баночки {emotionType}: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Обновляет кастомизацию баночки
        /// </summary>
        public async Task UpdateJarCustomization(string emotionType, JarCustomization customization)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления кастомизации баночки", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(emotionType))
                {
                    throw new ArgumentException("Тип эмоции не может быть пустым", nameof(emotionType));
                }

                if (customization == null)
                {
                    throw new ArgumentNullException(nameof(customization), "Данные кастомизации не могут быть null");
                }

                // Сериализуем данные кастомизации
                string json = JsonConvert.SerializeObject(customization);
                var customizationRef = _database.Child("users").Child(_userId).Child("jars").Child(emotionType.ToLower()).Child("customization");
                await customizationRef.SetRawJsonValueAsync(json);

                MyLogger.Log($"Кастомизация баночки {emotionType} успешно обновлена", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении кастомизации баночки {emotionType}: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Прослушивает данные баночек пользователя
        /// </summary>
        public void ListenToJars(Action<Dictionary<string, JarData>> onUpdate)
        {
            if (!CheckAuthentication()) return;
            var reference = _database.Child("users").Child(_userId).Child("jars");
            SubscribeToData(reference, onUpdate);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Создает банки по умолчанию для нового пользователя
        /// </summary>
        private async Task<Dictionary<string, JarData>> CreateDefaultJars()
        {
            try
            {
                if (!CheckAuthentication())
                {
                    MyLogger.LogError("Пользователь не авторизован для создания баночек", MyLogger.LogCategory.Firebase);
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

                    jarData[type.ToString().ToLower()] = jar;
                    
                    // Сохраняем в базу данных
                    await _database.Child("users").Child(_userId).Child("jars").Child(type.ToString().ToLower())
                        .SetRawJsonValueAsync(JsonConvert.SerializeObject(jar));
                }

                MyLogger.Log($"Созданы баночки по умолчанию для пользователя {_userId}", MyLogger.LogCategory.Firebase);
                return jarData;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при создании банок по умолчанию: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Рассчитывает емкость баночки в зависимости от уровня
        /// </summary>
        private int CalculateCapacityForLevel(int level)
        {
            // Простая формула для расчета емкости: 100 * уровень
            return 100 * level;
        }
        #endregion
    }
} 