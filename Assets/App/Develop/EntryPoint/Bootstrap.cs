using System;
using System.Collections;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.Firebase.Database.Services;
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

            MyLogger.Log("🚀 Запуск Bootstrap...", MyLogger.LogCategory.Bootstrap);

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

                                // Обновляем ID пользователя в сервисе базы данных                databaseService.UpdateUserId(user.UserId);                MyLogger.Log("✅ Пользователь аутентифицирован. Синхронизация истории будет выполняться при открытии панели истории.", MyLogger.LogCategory.Bootstrap);

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
                MyLogger.Log("🔐 Пользователь не авторизован. Переход в AuthScene.", MyLogger.LogCategory.Bootstrap);
                sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
            }
        }
    }
} 