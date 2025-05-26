using System;
using System.Collections;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.DI;
using App.Develop.Scenes.AuthScene;
using App.Develop.Scenes.PersonalAreaScene;
using Firebase.Auth;
using UnityEngine;
using UserProfile = App.Develop.CommonServices.Firebase.Database.Models.UserProfile;
using App.Develop.Utils.Logging;

namespace App.Develop.EntryPoint
{
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            var sceneSwitcher = container.Resolve<SceneSwitcher>();
            var auth = container.Resolve<FirebaseAuth>();
            var databaseService = container.Resolve<DatabaseService>();

            Debug.Log("🚀 [BOOTSTRAP-FORCE] Запуск Bootstrap...");
            MyLogger.Log("🚀 Запуск Bootstrap...", MyLogger.LogCategory.Bootstrap);

            Debug.Log($"🚀 [BOOTSTRAP-FORCE] Проверяем CurrentUser: {(auth.CurrentUser != null ? "ЕСТЬ" : "НЕТ")}");
            
            if (auth.CurrentUser != null)
            {
                // Проверяем флаг явного выхода - если пользователь сам вышел из системы,
                // то направляем его на экран авторизации
                bool explicitLogout = SecurePlayerPrefs.GetBool("explicit_logout", false);
                if (explicitLogout)
                {
                    MyLogger.Log("⚠️ Обнаружен флаг явного выхода из системы. Переход к экрану авторизации.", MyLogger.LogCategory.Bootstrap);
                    // Сбрасываем флаг
                    SecurePlayerPrefs.SetBool("explicit_logout", false);
                    SecurePlayerPrefs.Save();
                    
                    auth.SignOut();
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                var user = auth.CurrentUser;
                MyLogger.Log($"Найден пользователь: {user.Email}. Проверка сессии...", MyLogger.LogCategory.Bootstrap);

                // Проверяем сессию
                MyLogger.Log("Проверяем сессию...", MyLogger.LogCategory.Bootstrap);
                bool sessionValid = true;

                // Вынесем yield return за пределы try-catch
                var reloadTask = user.ReloadAsync();

                while (!reloadTask.IsCompleted)
                {
                    yield return null;
                }

                try
                {
                    if (reloadTask.IsFaulted || reloadTask.IsCanceled)
                    {
                        MyLogger.LogError("⚠️ Ошибка обновления данных пользователя", MyLogger.LogCategory.Bootstrap);
                        sessionValid = false;
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ Ошибка при проверке сессии: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                    sessionValid = false;
                }

                if (!sessionValid)
                {
                    MyLogger.Log("⚠️ Сессия недействительна. Выход и переход к авторизации.", MyLogger.LogCategory.Bootstrap);
                    auth.SignOut();
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                                // Обновляем ID пользователя в сервисе базы данных
                databaseService.UpdateUserId(user.UserId);
                
                // Проверяем активные сессии - предотвращаем одновременный вход с разных устройств
                Debug.Log("🔍 [BOOTSTRAP-FORCE] Проверяем активные сессии для предотвращения одновременного входа...");
                MyLogger.Log("🔍 Проверяем активные сессии для предотвращения одновременного входа...", MyLogger.LogCategory.Bootstrap);
                
                string currentDeviceId = ActiveSessionData.GetCurrentDeviceId();
                Debug.Log($"🔍 [BOOTSTRAP-FORCE] Текущий Device ID: {currentDeviceId}");
                MyLogger.Log($"🔍 Текущий Device ID: {currentDeviceId}", MyLogger.LogCategory.Bootstrap);
                
                if (string.IsNullOrEmpty(currentDeviceId))
                {
                    MyLogger.LogError("❌ Не удалось получить уникальный идентификатор устройства", MyLogger.LogCategory.Bootstrap);
                    auth.SignOut();
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }
                
                // Проверяем существование активной сессии с другого устройства
                var sessionCheckTask = databaseService.CheckActiveSessionExists(currentDeviceId);
                while (!sessionCheckTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (sessionCheckTask.IsFaulted)
                {
                    Debug.Log($"❌ [BOOTSTRAP-FORCE] Ошибка при проверке активных сессий: {sessionCheckTask.Exception?.Message}");
                    MyLogger.LogError($"❌ Ошибка при проверке активных сессий: {sessionCheckTask.Exception?.Message}", MyLogger.LogCategory.Bootstrap);
                    // Продолжаем выполнение, так как это не критическая ошибка
                }
                else if (sessionCheckTask.Result)
                {
                    Debug.Log("⚠️ [BOOTSTRAP-FORCE] Обнаружена активная сессия с другого устройства. Принудительный выход.");
                    MyLogger.Log("⚠️ Обнаружена активная сессия с другого устройства. Принудительный выход.", MyLogger.LogCategory.Bootstrap);
                    auth.SignOut();
                    databaseService.UpdateUserId(null);
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }
                else
                {
                    Debug.Log("✅ [BOOTSTRAP-FORCE] Других активных сессий не найдено. Продолжаем...");
                }
                
                // Регистрируем активную сессию для текущего устройства
                Debug.Log("🔍 [BOOTSTRAP-FORCE] Регистрируем активную сессию для текущего устройства...");
                MyLogger.Log("🔍 Регистрируем активную сессию для текущего устройства...", MyLogger.LogCategory.Bootstrap);
                var sessionRegisterTask = databaseService.RegisterActiveSession();
                while (!sessionRegisterTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (sessionRegisterTask.IsFaulted)
                {
                    Debug.Log($"❌ [BOOTSTRAP-FORCE] Ошибка при регистрации активной сессии: {sessionRegisterTask.Exception?.Message}");
                    MyLogger.LogError($"❌ Ошибка при регистрации активной сессии: {sessionRegisterTask.Exception?.Message}", MyLogger.LogCategory.Bootstrap);
                    // Продолжаем выполнение, так как это не критическая ошибка
                }
                else if (sessionRegisterTask.Result)
                {
                    Debug.Log("✅ [BOOTSTRAP-FORCE] Активная сессия зарегистрирована");
                    MyLogger.Log("✅ Активная сессия зарегистрирована", MyLogger.LogCategory.Bootstrap);
                }
                else
                {
                    Debug.Log("❌ [BOOTSTRAP-FORCE] Не удалось зарегистрировать активную сессию (Result = false)");
                }
                
                MyLogger.Log("✅ Пользователь аутентифицирован. Синхронизация истории будет выполняться при открытии панели истории.", MyLogger.LogCategory.Bootstrap);

                // Проверяем верификацию email
                if (!user.IsEmailVerified)
                {
                    MyLogger.Log("📧 Email не подтверждён. Переход в AuthScene → EmailVerification.", MyLogger.LogCategory.Bootstrap);
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                // Проверяем профиль пользователя
                MyLogger.Log("Загружаем профиль пользователя...", MyLogger.LogCategory.Bootstrap);
                UserProfile profile = null;

                // Вынесем операцию получения профиля из try-catch
                var profileTask = databaseService.GetUserProfile(user.UserId);

                while (!profileTask.IsCompleted)
                {
                    yield return null;
                }

                try
                {
                    if (profileTask.IsFaulted)
                    {
                        MyLogger.LogError($"❌ Ошибка при загрузке профиля: {profileTask.Exception?.InnerException?.Message}", MyLogger.LogCategory.Bootstrap);

                        throw profileTask.Exception?.InnerException ??
                              new Exception("Неизвестная ошибка при загрузке профиля");
                    }

                    profile = profileTask.Result;
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ Ошибка при загрузке профиля: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                    auth.SignOut();
                    databaseService.UpdateUserId(null);
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                if (profile == null)
                {
                    MyLogger.Log("👤 Профиль не найден. Переход в AuthScene → заполнение профиля.", MyLogger.LogCategory.Bootstrap);
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                if (string.IsNullOrEmpty(profile.Nickname))
                {
                    MyLogger.Log("👤 Никнейм не заполнен. Переход в AuthScene → заполнение профиля.", MyLogger.LogCategory.Bootstrap);
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                MyLogger.Log($"✅ Профиль загружен для пользователя {profile.Nickname}. Переход в PersonalArea.", MyLogger.LogCategory.Bootstrap);
                sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new PersonalAreaInputArgs()));
            }
            else
            {
                Debug.Log("🔐 [BOOTSTRAP-FORCE] Пользователь не авторизован. Переход в AuthScene.");
                MyLogger.Log("🔐 Пользователь не авторизован. Переход в AuthScene.", MyLogger.LogCategory.Bootstrap);
                sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
            }
        }
    }
} 