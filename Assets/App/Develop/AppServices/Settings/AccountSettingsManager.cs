using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;

namespace App.Develop.AppServices.Settings
{
    public class AccountSettingsManager : MonoBehaviour, IInjectable
    {
        [Header("UI")]
        [SerializeField] private GameObject _popupPanel;

        [SerializeField] private TMP_Text _popupText;

        [Header("Delete Confirmation")]
        [SerializeField] private GameObject _confirmDeletePanel;

        [SerializeField] private TMP_InputField _passwordConfirmInput;

        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;

            _confirmDeletePanel.SetActive(false); // по умолчанию скрыта
        }

        public void Logout()
        {
            _auth.SignOut();
            ShowPopup("Вы вышли из аккаунта.");

            _sceneSwitcher.ProcessSwitchSceneFor(
                new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        public void ShowDeleteConfirmation()
        {
            _confirmDeletePanel.SetActive(true);
        }

        public void CancelDelete()
        {
            _confirmDeletePanel.SetActive(false);
        }

        public void ConfirmDelete()
        {
            Debug.Log("🔎 Начало ConfirmDelete");

            if (_passwordConfirmInput == null)
            {
                Debug.LogError("❌ _passwordConfirmInput не привязан!");
                return;
            }

            string password = _passwordConfirmInput.text.Trim();
            Debug.Log($"🔐 Введённый пароль: '{password}'");

            if (string.IsNullOrEmpty(password))
            {
                ShowPopup("Введите пароль для подтверждения.");
                Debug.LogWarning("⚠️ Пароль пустой.");
                return;
            }

            if (_auth == null)
            {
                Debug.LogError("❌ _auth (FirebaseAuth) не инициализирован!");
                return;
            }

            FirebaseUser user = _auth.CurrentUser;

            if (user == null)
            {
                ShowPopup("Пользователь не найден. Повторите вход.");
                Debug.LogError("❌ CurrentUser == null");
                return;
            }

            string email = user.Email;
            Debug.Log($"📧 Email пользователя: {email}");

            var credential = EmailAuthProvider.GetCredential(email, password);

            Debug.Log("🔁 Начинаем повторную авторизацию...");

            user.ReauthenticateAsync(credential).ContinueWithOnMainThread(reAuthTask =>
            {
                if (!reAuthTask.IsCompletedSuccessfully)
                {
                    ShowPopup("Неверный пароль. Повторите попытку.");
                    Debug.LogError("❌ Reauth failed: " + reAuthTask.Exception?.Message);
                    return;
                }

                Debug.Log("✅ Повторная авторизация успешна. Удаляем документ из Firestore...");

                _db.Collection("users").Document(user.UserId)
                    .DeleteAsync().ContinueWithOnMainThread(docTask =>
                    {
                        if (!docTask.IsCompletedSuccessfully)
                        {
                            Debug.LogWarning("⚠️ Firestore delete failed: " + docTask.Exception?.Message);
                        }

                        Debug.Log("🗑 Удаляем аккаунт пользователя...");

                        user.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                        {
                            if (deleteTask.IsCompletedSuccessfully)
                            {
                                ShowPopup("Аккаунт удалён.");
                                Debug.Log("✅ Аккаунт успешно удалён.");

                                _sceneSwitcher.ProcessSwitchSceneFor(
                                    new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
                            }
                            else
                            {
                                ShowPopup("Ошибка при удалении аккаунта.");
                                Debug.LogError("❌ Delete failed: " + deleteTask.Exception?.Message);
                            }
                        });
                    });
            });
        }


        private void ShowPopup(string message)
        {
            _popupPanel.SetActive(true);
            _popupText.text = message;
            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 3f);
        }

        private void HidePopup()
        {
            _popupPanel.SetActive(false);
        }
    }
}
