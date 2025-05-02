using System.Collections;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using App.Develop.AppServices.Firebase.Auth.Services;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    public class AccountDeletionManager : MonoBehaviour, IInjectable
    {
        [Header("Panels")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        [SerializeField] private GameObject _confirmDeletePanel;
        [SerializeField] private Button _closePopupButton;

        [Header("Controls")]
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _showDeleteButton;
        [SerializeField] private Button _cancelDeleteButton;
        [SerializeField] private Button _confirmDeleteButton;
        [SerializeField] private Toggle _showPasswordToggle;
        [SerializeField] private TMP_InputField _passwordInput;

        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;
        private DatabaseReference _database;
        private AuthStateService _authStateService;
        private string _plainPassword = "";

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
            _database = container.Resolve<DatabaseReference>();
            _authStateService = container.Resolve<AuthStateService>();

            InitializeUI();
        }

        private void Start()
        {
            // Проверяем состояние аутентификации при запуске
            CheckAuthenticationState();
            // Регистрируем обработчик изменения состояния аутентификации
            _authStateService.AuthStateChanged += OnAuthStateChanged;
        }

        private void OnDestroy()
        {
            // Отписываемся от события при уничтожении объекта
            if (_authStateService != null)
            {
                _authStateService.AuthStateChanged -= OnAuthStateChanged;
            }
        }

        private void OnAuthStateChanged(FirebaseUser user)
        {
            // Обновляем ссылку на Auth при изменении состояния аутентификации
            _auth = FirebaseAuth.DefaultInstance;
            
            if (user == null)
            {
                Debug.LogWarning("⚠️ Пользователь вышел из системы или сессия истекла");
                ShowPopup("Ваша сессия истекла. Пожалуйста, войдите снова.");
                
                // Задержка перед редиректом, чтобы пользователь успел прочитать сообщение
                StartCoroutine(DelayedRedirect());
            }
            else
            {
                Debug.Log($"✅ Состояние аутентификации обновлено: {user.Email}");
            }
        }
        
        private IEnumerator DelayedRedirect()
        {
            yield return new WaitForSeconds(2f);
            _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        private void CheckAuthenticationState()
        {
            // Проверяем состояние аутентификации и при необходимости восстанавливаем
            StartCoroutine(CheckAndRestoreAuthenticationState());
        }
        
        private IEnumerator CheckAndRestoreAuthenticationState()
        {
            // Проверяем, авторизован ли пользователь
            if (!_authStateService.IsAuthenticated)
            {
                Debug.LogWarning("⚠️ Пользователь не авторизован при открытии AccountDeletionManager");
                
                // Пытаемся восстановить аутентификацию
                var restoreTask = _authStateService.RestoreAuthenticationAsync();
                
                // Ждем завершения восстановления
                while (!restoreTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (restoreTask.Result)
                {
                    Debug.Log("✅ Аутентификация успешно восстановлена");
                    // Обновляем ссылку наAuth
                    _auth = FirebaseAuth.DefaultInstance;
                }
                else
                {
                    Debug.LogError("❌ Не удалось восстановить аутентификацию");
                    ShowPopup("Ваша сессия истекла. Пожалуйста, войдите снова.");
                    yield return new WaitForSeconds(2f);
                    _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
                }
            }
            else
            {
                Debug.Log($"✅ Пользователь авторизован: {_authStateService.CurrentUser.Email}");
                // Обновляем информацию о пользователе
                yield return StartCoroutine(RefreshUserCoroutine());
            }
        }
        
        private IEnumerator RefreshUserCoroutine()
        {
            var refreshTask = _authStateService.RefreshUserAsync();
            
            while (!refreshTask.IsCompleted)
            {
                yield return null;
            }
            
            if (!refreshTask.Result)
            {
                Debug.LogWarning("⚠️ Не удалось обновить информацию о пользователе");
            }
        }

        private void InitializeUI()
        {
            Debug.Log("✅ InitializeUI вызван");

            if (_logoutButton == null) Debug.LogError("❌ _logoutButton не установлен!");
            if (_showDeleteButton == null) Debug.LogError("❌ _showDeleteButton не установлен!");
            if (_confirmDeleteButton == null) Debug.LogError("❌ _confirmDeleteButton не установлен!");
            if (_passwordInput == null) Debug.LogError("❌ _passwordInput не установлен!");
            if (_popupPanel == null || _popupText == null) Debug.LogError("❌ Popup элементы не установлены!");
            if (_closePopupButton == null) Debug.LogWarning("⚠️ _closePopupButton не установлен, всплывающие сообщения будут закрываться автоматически");

            _confirmDeletePanel.SetActive(false);
            _popupPanel.SetActive(false);

            SetupButtons();
            SetupToggles();
            SetupInputFields();

            SetPasswordVisibility(false);
        }

        private void SetupButtons()
        {
            _logoutButton.onClick.RemoveAllListeners();
            _logoutButton.onClick.AddListener(Logout);

            _showDeleteButton.onClick.RemoveAllListeners();
            _showDeleteButton.onClick.AddListener(ShowDeleteConfirmation);

            _cancelDeleteButton.onClick.RemoveAllListeners();
            _cancelDeleteButton.onClick.AddListener(CancelDelete);

            _confirmDeleteButton.onClick.RemoveAllListeners();
            _confirmDeleteButton.onClick.AddListener(ConfirmDelete);
            
            if (_closePopupButton != null)
            {
                _closePopupButton.onClick.RemoveAllListeners();
                _closePopupButton.onClick.AddListener(HidePopup);
            }
        }

        private void SetupToggles()
        {
            _showPasswordToggle.onValueChanged.RemoveAllListeners();
            _showPasswordToggle.isOn = false;
            _showPasswordToggle.onValueChanged.AddListener(OnToggleShowPassword);
        }

        private void SetupInputFields()
        {
            _passwordInput.onValueChanged.RemoveAllListeners();
            _passwordInput.onValueChanged.AddListener(OnPasswordChanged);
            _passwordInput.contentType = TMP_InputField.ContentType.Password;
            _passwordInput.ForceLabelUpdate();
        }

        private void OnPasswordChanged(string newText)
        {
            Debug.Log($"⌨ Введённый пароль изменён: {newText}");
            _plainPassword = newText;
        }

        private void OnToggleShowPassword(bool isVisible)
        {
            Debug.Log($"🔁 Toggle пароль: {(isVisible ? "Показать" : "Скрыть")}");
            SetPasswordVisibility(isVisible);
        }

        private void SetPasswordVisibility(bool isVisible)
        {
            _passwordInput.DeactivateInputField();

            _passwordInput.contentType = isVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

            StartCoroutine(RefreshPasswordField());
        }

        private IEnumerator RefreshPasswordField()
        {
            _passwordInput.text = "";
            _passwordInput.ForceLabelUpdate();
            yield return new WaitForEndOfFrame();
            _passwordInput.text = _plainPassword;
            _passwordInput.ForceLabelUpdate();
            _passwordInput.caretPosition = _plainPassword.Length;
            _passwordInput.ActivateInputField();
        }

        private void Logout()
        {
            Debug.Log("🔘 Logout нажата");

            _auth.SignOut();
            ShowPopup("Вы вышли из аккаунта.");

            _sceneSwitcher.ProcessSwitchSceneFor(
                new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        private void ShowDeleteConfirmation()
        {
            Debug.Log("🔘 Показать подтверждение удаления");
            
            // Проверяем текущее состояние аутентификации
            if (!_authStateService.IsAuthenticated)
            {
                Debug.LogError("❌ Пользователь не авторизован при открытии панели удаления");
                ShowPopup("Для удаления аккаунта необходимо войти в систему.");
                StartCoroutine(DelayedRedirect());
                return;
            }
            
            _confirmDeletePanel.SetActive(true);
            SetPasswordVisibility(_showPasswordToggle.isOn);
        }

        private void CancelDelete()
        {
            Debug.Log("🔘 Отмена удаления");
            _confirmDeletePanel.SetActive(false);
        }

        private void ConfirmDelete()
        {
            Debug.Log("🔘 Подтвердить удаление");
            
            // Проверка на пустой пароль
            if (string.IsNullOrWhiteSpace(_plainPassword))
            {
                Debug.LogWarning("⚠️ Пустой пароль при попытке удаления аккаунта");
                ShowPopup("Введите пароль для подтверждения.");
                return;
            }
            
            // Проверяем авторизацию через AuthStateService
            if (!_authStateService.IsAuthenticated)
            {
                Debug.LogError("❌ Пользователь не авторизован при попытке удаления аккаунта");
                ShowPopup("Для удаления аккаунта необходимо войти в систему.");
                StartCoroutine(DelayedRedirect());
                return;
            }
            
            // Получаем текущего пользователя из сервиса
            var user = _authStateService.CurrentUser;
            var email = user.Email;
            
            Debug.Log($"📧 Текущий пользователь: {email ?? "null"}, UID: {user.UserId}");
            
            if (string.IsNullOrEmpty(email))
            {
                Debug.LogError($"❌ Email пользователя отсутствует. UID: {user.UserId}");
                ShowPopup("Ошибка: не удалось получить email. Повторите вход в аккаунт.");
                return;
            }
            
            try
            {
                // Получаем учетные данные
                var credential = EmailAuthProvider.GetCredential(email, _plainPassword);
                
                // Выполняем повторную аутентификацию
                user.ReauthenticateAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"❌ Ошибка реаутентификации: {task.Exception?.GetBaseException()?.Message}");
                        ShowPopup("Неверный пароль или ошибка аутентификации.");
                        return;
                    }
                    
                    // Удаляем данные пользователя
                    DeleteUserData();
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Исключение при аутентификации: {ex.Message}");
                ShowPopup("Произошла ошибка. Повторите попытку позже.");
            }
        }

        private void DeleteUserData()
        {
            // Проверяем авторизацию через AuthStateService
            if (!_authStateService.IsAuthenticated)
            {
                Debug.LogError("❌ Пользователь не авторизован при удалении данных");
                ShowPopup("Ошибка: сессия истекла. Войдите снова.");
                StartCoroutine(DelayedRedirect());
                return;
            }
            
            var uid = _authStateService.CurrentUser.UserId;
            
            Debug.Log($"🗑️ Удаление данных пользователя: {uid}");
            
            _database
                .Child("users")
                .Child(uid)
                .RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"❌ Ошибка при удалении данных: {task.Exception?.GetBaseException()?.Message}");
                        
                        // Можно продолжить с удалением аккаунта, даже если данные не удалились
                        Debug.LogWarning("⚠️ Продолжаем с удалением аккаунта, несмотря на ошибку с базой данных");
                    }
                    else
                    {
                        Debug.Log("✅ Данные пользователя успешно удалены");
                    }
                    
                    // Проверяем авторизацию перед удалением аккаунта
                    if (!_authStateService.IsAuthenticated)
                    {
                        Debug.LogError("❌ Сессия истекла после удаления данных");
                        ShowPopup("Ошибка: сессия истекла в процессе удаления. Данные удалены, но аккаунт сохранен.");
                        StartCoroutine(DelayedRedirect());
                        return;
                    }
                    
                    DeleteFirebaseUser();
                });
        }

        private void DeleteFirebaseUser()
        {
            // Проверяем авторизацию через AuthStateService
            if (!_authStateService.IsAuthenticated)
            {
                Debug.LogError("❌ Пользователь не авторизован при удалении учетной записи");
                ShowPopup("Ошибка: сессия истекла. Войдите снова.");
                StartCoroutine(DelayedRedirect());
                return;
            }
            
            Debug.Log($"🗑️ Удаление аккаунта Firebase: {_authStateService.CurrentUser.Email}");
            
            _authStateService.CurrentUser.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Ошибка при удалении аккаунта: {task.Exception?.GetBaseException()?.Message}");
                    
                    bool requiresReauth = task.Exception?.InnerExceptions.Any(ex => 
                        ex.Message.Contains("requires recent authentication") || 
                        ex.Message.Contains("requires a recent login")) ?? false;
                    
                    if (requiresReauth)
                    {
                        ShowPopup("Для удаления аккаунта необходима повторная авторизация. Пожалуйста, войдите снова.");
                        StartCoroutine(DelayedRedirect());
                    }
                    else
                    {
                        ShowPopup("Не удалось удалить аккаунт. Повторите попытку позже.");
                    }
                    
                    return;
                }
                
                Debug.Log("✅ Аккаунт успешно удален");
                CleanupStoredCredentials();
                ShowPopup("Аккаунт успешно удален.");
                
                _sceneSwitcher.ProcessSwitchSceneFor(
                    new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
            });
        }

        private void CleanupStoredCredentials()
        {
            SecurePlayerPrefs.DeleteKey("email");
            SecurePlayerPrefs.DeleteKey("password");
            SecurePlayerPrefs.DeleteKey("remember_me");
            SecurePlayerPrefs.Save();
        }

        private void ShowPopup(string message)
        {
            if (_popupPanel == null || _popupText == null) return;
            
            Debug.Log($"📢 Показ сообщения: {message}");
            _popupText.text = message;
            _popupPanel.SetActive(true);
            
            CancelInvoke(nameof(HidePopup));
            
            if (_closePopupButton == null)
            {
                // Если нет кнопки закрытия, скрываем автоматически через 5 секунд
                Invoke(nameof(HidePopup), 5f);
            }
        }

        private void HidePopup()
        {
            Debug.Log("🔍 Скрытие всплывающего сообщения");
            _popupPanel?.SetActive(false);
        }
    }
}
