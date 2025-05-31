using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для управления сессиями пользователя в Firebase Database
    /// </summary>
    public class SessionManagementService : FirebaseDatabaseServiceBase, ISessionManagementService
    {
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса управления сессиями
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public SessionManagementService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null) 
            : base(database, cacheManager, validationService)
        {
            MyLogger.Log("✅ SessionManagementService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region ISessionManagementService Implementation
        /// <summary>
        /// Получает информацию о всех активных сессиях пользователя
        /// </summary>
        public async Task<Dictionary<string, ActiveSessionData>> GetActiveSessions()
        {
            if (!CheckAuthentication())
            {
                return new Dictionary<string, ActiveSessionData>();
            }

            try
            {
                var activeSessionsRef = _database.Child("users").Child(_userId).Child("activeSessions");
                var snapshot = await activeSessionsRef.GetValueAsync();

                if (!snapshot.Exists)
                {
                    MyLogger.Log($"📱 Активные сессии не найдены для пользователя {_userId}", MyLogger.LogCategory.Firebase);
                    return new Dictionary<string, ActiveSessionData>();
                }

                var sessions = new Dictionary<string, ActiveSessionData>();
                
                foreach (var childSnapshot in snapshot.Children)
                {
                    try
                    {
                        string deviceId = childSnapshot.Key;
                        var sessionData = new ActiveSessionData
                        {
                            DeviceId = deviceId,
                            DeviceInfo = childSnapshot.Child("deviceInfo").Value?.ToString(),
                            IpAddress = childSnapshot.Child("ipAddress").Value?.ToString(),
                            LastActivityTimestamp = Convert.ToInt64(childSnapshot.Child("lastActivityTimestamp").Value ?? 0)
                        };
                        
                        sessions[deviceId] = sessionData;
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ Ошибка при обработке сессии {childSnapshot.Key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                    }
                }
                
                MyLogger.Log($"📱 Получено {sessions.Count} активных сессий для пользователя {_userId}", MyLogger.LogCategory.Firebase);
                return sessions;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при получении активных сессий: {ex.Message}", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, ActiveSessionData>();
            }
        }

        /// <summary>
        /// Регистрирует новую активную сессию для текущего устройства
        /// </summary>
        public async Task<bool> RegisterActiveSession()
        {
            Debug.Log("🔍 [SESSION-REGISTER-FORCE] Начинаем регистрацию активной сессии...");
            
            if (!CheckAuthentication())
            {
                Debug.Log("❌ [SESSION-REGISTER-FORCE] Пользователь не аутентифицирован при регистрации сессии");
                MyLogger.LogWarning("❌ [SESSION-REGISTER] Пользователь не аутентифицирован при регистрации сессии", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                // Создаем данные о текущем устройстве
                var sessionData = ActiveSessionData.CreateFromCurrentDevice();
                string deviceId = sessionData.DeviceId;
                
                Debug.Log($"🔍 [SESSION-REGISTER-FORCE] Подготовлена сессия для устройства: {deviceId}, Информация: {sessionData.DeviceInfo}");
                MyLogger.Log($"🔍 [SESSION-REGISTER] Подготовлена сессия для устройства: {deviceId}, Информация: {sessionData.DeviceInfo}", MyLogger.LogCategory.Firebase);
                
                // Проверяем, что deviceId не пустой
                if (string.IsNullOrEmpty(deviceId))
                {
                    MyLogger.LogError("❌ [SESSION-REGISTER] DeviceId не может быть пустым при регистрации сессии", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                // Проверяем, что путь к узлу activeSessions существует
                var databasePath = $"users/{_userId}/activeSessions/{deviceId}";
                MyLogger.Log($"🔍 [SESSION-REGISTER] Путь в базе данных: {databasePath}", MyLogger.LogCategory.Firebase);
                
                // Получаем ссылку на узел activeSessions для проверки доступа
                var activeSessionsRef = _database.Child("users").Child(_userId).Child("activeSessions");
                
                try
                {
                    // Проверяем права доступа
                    var testSnapshot = await activeSessionsRef.GetValueAsync();
                    MyLogger.Log($"🔍 [SESSION-REGISTER] Проверка доступа к узлу: {(testSnapshot != null ? "Успешно" : "Не удалось")}", MyLogger.LogCategory.Firebase);
                }
                catch (Exception checkEx)
                {
                    MyLogger.LogError($"❌ [SESSION-REGISTER] Ошибка при проверке доступа: {checkEx.Message}", MyLogger.LogCategory.Firebase);
                    // Продолжаем выполнение, так как узел может быть еще не создан
                }
                
                // Сохраняем данные в Firebase (используем deviceId как ключ)
                Debug.Log($"🔍 [SESSION-REGISTER-FORCE] Начинаем сохранение в Firebase по пути: {databasePath}");
                var saveTask = activeSessionsRef.Child(deviceId).SetValueAsync(sessionData.ToDictionary());
                
                Debug.Log($"🔍 [SESSION-REGISTER-FORCE] Задача создана, начинаем await...");
                
                try
                {
                    await saveTask;
                    Debug.Log($"🔍 [SESSION-REGISTER-FORCE] await завершен успешно!");
                }
                catch (Exception awaitEx)
                {
                    Debug.Log($"❌ [SESSION-REGISTER-FORCE] Исключение при await: {awaitEx.Message}");
                    Debug.Log($"❌ [SESSION-REGISTER-FORCE] StackTrace: {awaitEx.StackTrace}");
                    
                    // Получаем детали внутренней ошибки Firebase
                    if (saveTask.Exception != null)
                    {
                        Debug.Log($"❌ [SESSION-REGISTER-FORCE] Firebase Task Exception: {saveTask.Exception.Message}");
                        if (saveTask.Exception.InnerException != null)
                        {
                            Debug.Log($"❌ [SESSION-REGISTER-FORCE] Firebase Inner Exception: {saveTask.Exception.InnerException.Message}");
                            Debug.Log($"❌ [SESSION-REGISTER-FORCE] Firebase Inner StackTrace: {saveTask.Exception.InnerException.StackTrace}");
                        }
                        
                        // Проверяем все внутренние исключения
                        if (saveTask.Exception is AggregateException aggEx)
                        {
                            int level = 0;
                            foreach (var innerEx in aggEx.InnerExceptions)
                            {
                                Debug.Log($"❌ [SESSION-REGISTER-FORCE] AggregateException Level {level}: {innerEx.GetType().Name} - {innerEx.Message}");
                                level++;
                            }
                        }
                    }
                    
                    MyLogger.LogError($"❌ [SESSION-REGISTER] Исключение при await: {awaitEx.Message}", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                Debug.Log($"🔍 [SESSION-REGISTER-FORCE] Задача сохранения завершена. IsFaulted: {saveTask.IsFaulted}, IsCompleted: {saveTask.IsCompleted}, IsCanceled: {saveTask.IsCanceled}");
                
                if (saveTask.IsFaulted)
                {
                    Debug.Log($"❌ [SESSION-REGISTER-FORCE] Ошибка при сохранении сессии: {saveTask.Exception?.Message}");
                    MyLogger.LogError($"❌ [SESSION-REGISTER] Ошибка при сохранении сессии: {saveTask.Exception?.Message}", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                if (saveTask.IsCanceled)
                {
                    Debug.Log($"❌ [SESSION-REGISTER-FORCE] Задача сохранения была отменена");
                    MyLogger.LogError($"❌ [SESSION-REGISTER] Задача сохранения была отменена", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                // Проверяем что данные действительно сохранились
                Debug.Log($"🔍 [SESSION-REGISTER-FORCE] Проверяем что данные сохранились...");
                try
                {
                    var verificationSnapshot = await activeSessionsRef.Child(deviceId).GetValueAsync();
                    if (verificationSnapshot.Exists)
                    {
                        Debug.Log($"✅ [SESSION-REGISTER-FORCE] Данные подтверждены в Firebase!");
                        MyLogger.Log($"✅ [SESSION-REGISTER] Активная сессия зарегистрирована для устройства: {deviceId}", MyLogger.LogCategory.Firebase);
                        return true;
                    }
                    else
                    {
                        Debug.Log($"❌ [SESSION-REGISTER-FORCE] Данные НЕ найдены в Firebase после сохранения!");
                        MyLogger.LogError($"❌ [SESSION-REGISTER] Данные не сохранились в Firebase", MyLogger.LogCategory.Firebase);
                        return false;
                    }
                }
                catch (Exception verifyEx)
                {
                    Debug.Log($"❌ [SESSION-REGISTER-FORCE] Ошибка при проверке сохранения: {verifyEx.Message}");
                    MyLogger.LogError($"❌ [SESSION-REGISTER] Ошибка при проверке сохранения: {verifyEx.Message}", MyLogger.LogCategory.Firebase);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [SESSION-REGISTER] Ошибка при регистрации активной сессии: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [SESSION-REGISTER] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Очищает все активные сессии пользователя
        /// </summary>
        public async Task<bool> ClearActiveSessions()
        {
            if (!CheckAuthentication())
            {
                return false;
            }

            try
            {
                await _database.Child("users").Child(_userId).Child("activeSessions").RemoveValueAsync();
                MyLogger.Log($"✅ Все активные сессии очищены для пользователя: {_userId}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при очистке активных сессий: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Очищает активную сессию конкретного устройства
        /// </summary>
        public async Task<bool> ClearActiveSession(string deviceId)
        {
            if (!CheckAuthentication())
            {
                return false;
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                MyLogger.LogError("❌ DeviceId не может быть пустым при очистке сессии", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                await _database.Child("users").Child(_userId).Child("activeSessions").Child(deviceId).RemoveValueAsync();
                MyLogger.Log($"✅ Активная сессия очищена для устройства: {deviceId}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при очистке активной сессии для устройства {deviceId}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Проверяет существование активной сессии с другого устройства
        /// </summary>
        public async Task<bool> CheckActiveSessionExists(string currentDeviceId)
        {
            Debug.Log($"🔍 [SESSION-CHECK-FORCE] Начинаем проверку сессий для устройства {currentDeviceId}");
            
            if (!CheckAuthentication())
            {
                Debug.Log("❌ [SESSION-CHECK-FORCE] Пользователь не аутентифицирован при проверке сессий");
                MyLogger.LogWarning("❌ [SESSION-CHECK] Пользователь не аутентифицирован при проверке сессий", MyLogger.LogCategory.Firebase);
                return false;
            }

            if (string.IsNullOrEmpty(currentDeviceId))
            {
                Debug.Log("⚠️ [SESSION-CHECK-FORCE] Текущий DeviceId пустой при проверке активной сессии");
                MyLogger.LogWarning("⚠️ [SESSION-CHECK] Текущий DeviceId пустой при проверке активной сессии", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                Debug.Log($"🔍 [SESSION-CHECK-FORCE] Начинаем проверку сессий для устройства {currentDeviceId}");
                MyLogger.Log($"🔍 [SESSION-CHECK] Начинаем проверку сессий для устройства {currentDeviceId}", MyLogger.LogCategory.Firebase);
                
                // Получаем все активные сессии
                var sessions = await GetActiveSessions();
                
                Debug.Log($"🔍 [SESSION-CHECK-FORCE] Получено сессий: {sessions.Count}");
                MyLogger.Log($"🔍 [SESSION-CHECK] Получено сессий: {sessions.Count}", MyLogger.LogCategory.Firebase);
                
                // Выводим информацию о каждой сессии для отладки
                foreach (var session in sessions)
                {
                    MyLogger.Log($"🔍 [SESSION-CHECK] Сессия: DeviceId={session.Key}, " +
                        $"DeviceInfo={session.Value.DeviceInfo}, " +
                        $"LastActive={new DateTime(1970, 1, 1).AddMilliseconds(session.Value.LastActivityTimestamp)}", 
                        MyLogger.LogCategory.Firebase);
                }
                
                if (sessions.Count == 0)
                {
                    MyLogger.Log("📱 [SESSION-CHECK] Нет активных сессий для текущего пользователя", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                // Ищем сессии других устройств
                bool otherSessionExists = sessions.Any(pair => pair.Key != currentDeviceId);
                
                if (otherSessionExists)
                {
                    Debug.Log($"⚠️ [SESSION-CHECK-FORCE] Обнаружены активные сессии с других устройств. Всего сессий: {sessions.Count}");
                    MyLogger.Log($"⚠️ [SESSION-CHECK] Обнаружены активные сессии с других устройств. Всего сессий: {sessions.Count}", MyLogger.LogCategory.Firebase);
                    
                    // Выводим список устройств, отличных от текущего
                    var otherSessions = sessions.Where(pair => pair.Key != currentDeviceId).ToList();
                    foreach (var session in otherSessions)
                    {
                        Debug.Log($"⚠️ [SESSION-CHECK-FORCE] Другая сессия: DeviceId={session.Key}, DeviceInfo={session.Value.DeviceInfo}");
                        MyLogger.Log($"⚠️ [SESSION-CHECK] Другая сессия: DeviceId={session.Key}, DeviceInfo={session.Value.DeviceInfo}", 
                            MyLogger.LogCategory.Firebase);
                    }
                }
                else
                {
                    Debug.Log($"📱 [SESSION-CHECK-FORCE] Активные сессии других устройств не обнаружены. Всего сессий: {sessions.Count}");
                    MyLogger.Log($"📱 [SESSION-CHECK] Активные сессии других устройств не обнаружены. Всего сессий: {sessions.Count}", MyLogger.LogCategory.Firebase);
                }
                
                return otherSessionExists;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [SESSION-CHECK] Ошибка при проверке активных сессий: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [SESSION-CHECK] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Обновляет активную сессию для указанного устройства
        /// </summary>
        public async Task<bool> UpdateActiveSession(string deviceId)
        {
            if (!CheckAuthentication())
            {
                return false;
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                MyLogger.LogError("❌ DeviceId не может быть пустым при обновлении сессии", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                var sessionData = new ActiveSessionData
                {
                    DeviceId = deviceId,
                    DeviceInfo = SystemInfo.deviceModel,
                    LastActivityTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var activeSessionsRef = _database.Child("users").Child(_userId).Child("activeSessions").Child(deviceId);
                await activeSessionsRef.SetValueAsync(sessionData.ToDictionary());
                
                MyLogger.Log($"✅ Активная сессия обновлена для устройства: {deviceId}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении активной сессии для устройства {deviceId}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Проверяет существование пользователя в базе данных
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>True, если пользователь существует</returns>
        public async Task<bool> CheckUserExists(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                MyLogger.LogWarning("❌ UserId не может быть пустым при проверке существования пользователя", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                var userRef = _database.Child("users").Child(userId);
                var userSnapshot = await userRef.GetValueAsync();
                
                bool exists = userSnapshot.Exists;
                MyLogger.Log($"🔍 Проверка существования пользователя {userId}: {(exists ? "существует" : "не существует")}", MyLogger.LogCategory.Firebase);
                
                return exists;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при проверке существования пользователя {userId}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        #endregion
    }
} 