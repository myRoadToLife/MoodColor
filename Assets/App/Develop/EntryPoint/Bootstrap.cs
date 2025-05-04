// Assets/App/Develop/EntryPoint/Bootstrap.cs

using System;
using System.Collections;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Scenes.AuthScene;
using App.Develop.Scenes.PersonalAreaScene;
using Firebase.Auth;
using UnityEngine;
using UserProfile = App.Develop.CommonServices.Firebase.Database.Models.UserProfile;

namespace App.Develop.EntryPoint
{
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            var sceneSwitcher = container.Resolve<SceneSwitcher>();
            var auth = container.Resolve<FirebaseAuth>();
            var databaseService = container.Resolve<DatabaseService>();

            Debug.Log("🚀 Запуск Bootstrap...");

            if (auth.CurrentUser != null)
            {
                // Проверяем флаг явного выхода - если пользователь сам вышел из системы,
                // то направляем его на экран авторизации
                bool explicitLogout = SecurePlayerPrefs.GetBool("explicit_logout", false);
                if (explicitLogout)
                {
                    Debug.Log("⚠️ Обнаружен флаг явного выхода из системы. Переход к экрану авторизации.");
                    // Сбрасываем флаг
                    SecurePlayerPrefs.SetBool("explicit_logout", false);
                    SecurePlayerPrefs.Save();
                    
                    auth.SignOut();
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                var user = auth.CurrentUser;
                Debug.Log($"Найден пользователь: {user.Email}. Проверка сессии...");

                // Проверяем сессию
                Debug.Log("Проверяем сессию...");
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
                        Debug.LogError("⚠️ Ошибка обновления данных пользователя");
                        sessionValid = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка при проверке сессии: {ex.Message}");
                    sessionValid = false;
                }

                if (!sessionValid)
                {
                    Debug.Log("⚠️ Сессия недействительна. Выход и переход к авторизации.");
                    auth.SignOut();
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                // Обновляем ID пользователя в сервисе базы данных
                databaseService.UpdateUserId(user.UserId);

                // Проверяем верификацию email
                if (!user.IsEmailVerified)
                {
                    Debug.Log("📧 Email не подтверждён. Переход в AuthScene → EmailVerification.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                // Проверяем профиль пользователя
                Debug.Log("Загружаем профиль пользователя...");
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
                        Debug.LogError($"❌ Ошибка при загрузке профиля: {profileTask.Exception?.InnerException?.Message}");

                        throw profileTask.Exception?.InnerException ??
                              new Exception("Неизвестная ошибка при загрузке профиля");
                    }

                    profile = profileTask.Result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка при загрузке профиля: {ex.Message}");
                    auth.SignOut();
                    databaseService.UpdateUserId(null);
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                if (profile == null)
                {
                    Debug.Log("👤 Профиль не найден. Переход в AuthScene → заполнение профиля.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                if (string.IsNullOrEmpty(profile.Nickname))
                {
                    Debug.Log("👤 Никнейм не заполнен. Переход в AuthScene → заполнение профиля.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                Debug.Log($"✅ Профиль загружен для пользователя {profile.Nickname}. Переход в PersonalArea.");
                sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new PersonalAreaInputArgs()));
            }
            else
            {
                Debug.Log("🔐 Пользователь не авторизован. Переход в AuthScene.");
                sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
            }
        }
    }
}
