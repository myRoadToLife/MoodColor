using System.Collections;
using System.Threading.Tasks;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    public class Bootstrap : MonoBehaviour
    {
        public IEnumerator Run(DIContainer container)
        {
            SceneSwitcher sceneSwitcher = container.Resolve<SceneSwitcher>();
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            Debug.Log("🚀 Запуск Bootstrap...");

            if (auth.CurrentUser != null)
            {
                FirebaseUser user = auth.CurrentUser;
                Debug.Log($"Найден пользователь: {user.Email}. Проверка верификации...");

                Task reloadTask = user.ReloadAsync();
                yield return new WaitUntil(() => reloadTask.IsCompleted);

                if (reloadTask.IsFaulted || reloadTask.IsCanceled)
                {
                    Debug.LogWarning("⚠️ Сессия недействительна. Выход и возврат к AuthScene.");
                    auth.SignOut();
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                if (!user.IsEmailVerified)
                {
                    Debug.Log("📧 Email не подтверждён. Переход в AuthScene → EmailVerification.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                Task<DocumentSnapshot> userDocTask = db.Collection("users").Document(user.UserId).GetSnapshotAsync();
                yield return new WaitUntil(() => userDocTask.IsCompleted);

                if (userDocTask.IsFaulted || userDocTask.IsCanceled || !userDocTask.Result.Exists)
                {
                    Debug.Log("❓ Нет данных профиля. Переход в AuthScene → заполнение профиля.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                    yield break;
                }

                DocumentSnapshot doc = userDocTask.Result;

                bool hasNickname = doc.ContainsField("nickname");
                bool hasGender = doc.ContainsField("gender");

                if (hasNickname && hasGender)
                {
                    Debug.Log("✅ Профиль заполнен. Переход в PersonalArea.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new PersonalAreaInputArgs()));
                }
                else
                {
                    Debug.Log("👤 Профиль не заполнен. Переход в AuthScene → заполнение профиля.");
                    sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
                }
            }
            else
            {
                Debug.Log("🔐 Пользователь не авторизован. Переход в AuthScene.");
                sceneSwitcher.ProcessSwitchSceneFor(new OutputBootstrapArgs(new AuthSceneInputArgs()));
            }
        }
    }
}
