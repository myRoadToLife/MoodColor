using System.Collections;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
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
using App.Develop.CommonServices.Firebase.Auth.Services;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    public class AccountDeletionManager : MonoBehaviour, IInjectable
    {
        #region SerializeFields
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
        #endregion

        #region Private Fields
        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;
        private DatabaseReference _database;
        private IAuthStateService _authStateService;
        private AccountDeletionHelper _deletionHelper;
        private string _plainPassword = "";
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Debug.Log("✅ AccountDeletionManager.Start вызван. В случае ошибок, убедитесь, что инъекция произошла до Start.");
            
            // Вместо непосредственной инициализации, запускаем корутину, которая будет ждать,
            // пока все зависимости будут правильно инициализированы
            StartCoroutine(WaitForDependenciesAndInitialize());
        }

        private IEnumerator WaitForDependenciesAndInitialize()
        {
            // Ждем максимум 10 секунд для инициализации зависимостей
            float waitTime = 0f;
            float maxWaitTime = Mathf.Min(10f, Time.maximumDeltaTime * 30f);
            
            // Ждем, пока _authStateService не будет инициализирован или не истечет время ожидания
            while (_authStateService == null && waitTime < maxWaitTime)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            // После ожидания проверяем, был ли сервис инициализирован
            if (_authStateService == null)
            {
                Debug.LogError($"❌ _authStateService остался NULL после {waitTime} секунд ожидания! " +
                              "Проблема с инъекцией зависимостей при использовании Addressables.");
                // Возможно, стоит показать пользователю сообщение об ошибке или перезагрузить сцену
                ShowPopup("Произошла ошибка при загрузке. Пожалуйста, попробуйте позже.");
                yield break;
            }
            
            // Теперь, когда у нас есть все зависимости, можно инициализировать компонент
            Debug.Log($"✅ _authStateService успешно получен после {waitTime} секунд ожидания.");
            
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
            
            // Отписываемся от событий помощника удаления
            UnsubscribeFromHelperEvents();
        }
        #endregion

        #region Initialization
        public void Inject(DIContainer container)
        {
            Debug.Log("🚀 [AccountDeletionManager] Inject CALLED. Container is null: " + (container == null));

            try 
            {
                if (container == null) 
                {
                    Debug.LogError("❌ [AccountDeletionManager] Inject CALLED with a NULL container!");
                    throw new ArgumentNullException(nameof(container));
                }
                
                _sceneSwitcher = container.Resolve<SceneSwitcher>();
                if (_sceneSwitcher == null) Debug.LogError("❌ [AccountDeletionManager] Не удалось получить SceneSwitcher из контейнера!");
                
                _auth = FirebaseAuth.DefaultInstance;
                if (_auth == null) Debug.LogError("❌ [AccountDeletionManager] FirebaseAuth.DefaultInstance вернул NULL!");
                
                _database = container.Resolve<DatabaseReference>();
                if (_database == null) Debug.LogError("❌ [AccountDeletionManager] Не удалось получить DatabaseReference из контейнера!");
                
                // Пытаемся получить сервис и логируем результат
                IAuthStateService resolvedService = null;
                try
                {
                    resolvedService = container.Resolve<IAuthStateService>();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ [AccountDeletionManager] ОШИБКА при попытке Resolve<IAuthStateService>: {ex.Message}\n{ex.StackTrace}");
                }
                
                _authStateService = resolvedService;
                if (_authStateService == null) 
                {
                    Debug.LogError("❌ [AccountDeletionManager] IAuthStateService остался NULL после попытки Resolve! Это критическая ошибка.");
                    return; 
                }
                else
                {
                    Debug.Log("✅ [AccountDeletionManager] IAuthStateService УСПЕШНО получен из контейнера.");
                }
                
                _deletionHelper = new AccountDeletionHelper(_database, _authStateService);
                SubscribeToHelperEvents();

                InitializeUI();
                Debug.Log("✅ AccountDeletionManager успешно инициализирован через Inject");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Глобальная ошибка в Inject AccountDeletionManager: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void SubscribeToHelperEvents()
        {
            if (_deletionHelper == null) return;
            
            _deletionHelper.OnMessage += ShowPopup;
            _deletionHelper.OnError += ShowPopup;
            _deletionHelper.OnRedirectToAuth += () => StartCoroutine(DelayedRedirect());
            _deletionHelper.OnUserDeleted += () => 
            {
                ShowPopup("Аккаунт успешно удален.");
                _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
            };
        }
        
        private void UnsubscribeFromHelperEvents()
        {
            if (_deletionHelper == null) return;
            
            _deletionHelper.OnMessage -= ShowPopup;
            _deletionHelper.OnError -= ShowPopup;
            _deletionHelper.OnRedirectToAuth -= () => StartCoroutine(DelayedRedirect());
            _deletionHelper.OnUserDeleted -= () => 
            {
                ShowPopup("Аккаунт успешно удален.");
                _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
            };
        }
        #endregion

        #region Authentication State Management
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
            // Проверка инициализации сервиса
            if (_authStateService == null)
            {
                Debug.LogError("❌ _authStateService is NULL в CheckAuthenticationState!");
                return;
            }
            
            // Проверяем состояние аутентификации и при необходимости восстанавливаем
            StartCoroutine(CheckAndRestoreAuthenticationState());
        }
        
        private IEnumerator CheckAndRestoreAuthenticationState()
        {
            // Проверяем, что сервис аутентификации инициализирован
            if (_authStateService == null)
            {
                Debug.LogError("❌ _authStateService is NULL в CheckAndRestoreAuthenticationState! Инъекция не произошла или запущена слишком поздно.");
                yield break;
            }

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
        #endregion

        #region UI Initialization
        private void InitializeUI()
        {
            try 
            {
                Debug.Log("✅ InitializeUI вызван");

                // Проверяем все необходимые UI элементы
                bool hasErrors = false;
                
                if (_logoutButton == null) { Debug.LogError("❌ _logoutButton не установлен!"); hasErrors = true; }
                if (_showDeleteButton == null) { Debug.LogError("❌ _showDeleteButton не установлен!"); hasErrors = true; }
                if (_confirmDeleteButton == null) { Debug.LogError("❌ _confirmDeleteButton не установлен!"); hasErrors = true; }
                if (_cancelDeleteButton == null) { Debug.LogError("❌ _cancelDeleteButton не установлен!"); hasErrors = true; }
                if (_passwordInput == null) { Debug.LogError("❌ _passwordInput не установлен!"); hasErrors = true; }
                if (_showPasswordToggle == null) { Debug.LogError("❌ _showPasswordToggle не установлен!"); hasErrors = true; }
                if (_popupPanel == null) { Debug.LogError("❌ _popupPanel не установлен!"); hasErrors = true; }
                if (_popupText == null) { Debug.LogError("❌ _popupText не установлен!"); hasErrors = true; }
                
                if (_closePopupButton == null) 
                {
                    Debug.LogWarning("⚠️ _closePopupButton не установлен, всплывающие сообщения будут закрываться автоматически");
                }

                // Если есть ошибки с критическими UI элементами, выходим из метода
                if (hasErrors)
                {
                    Debug.LogError("❌ Критические UI элементы отсутствуют. Возможно, префаб не загружен полностью через Addressables.");
                    return;
                }

                // Безопасно скрываем панели
                if (_confirmDeletePanel != null) _confirmDeletePanel.SetActive(false);
                if (_popupPanel != null) _popupPanel.SetActive(false);

                // Настраиваем компоненты UI
                SetupButtons();
                SetupToggles();
                SetupInputFields();
                
                // Настраиваем видимость пароля
                SetPasswordVisibility(false);
                
                Debug.Log("✅ Интерфейс успешно инициализирован");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при инициализации UI: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #region UI Setup Methods
        private void SetupButtons()
        {
            try 
            {
                if (_logoutButton != null)
                {
                    _logoutButton.onClick.RemoveAllListeners();
                    _logoutButton.onClick.AddListener(Logout);
                }

                if (_showDeleteButton != null)
                {
                    _showDeleteButton.onClick.RemoveAllListeners();
                    _showDeleteButton.onClick.AddListener(ShowDeleteConfirmation);
                }

                if (_cancelDeleteButton != null)
                {
                    _cancelDeleteButton.onClick.RemoveAllListeners();
                    _cancelDeleteButton.onClick.AddListener(CancelDelete);
                }

                if (_confirmDeleteButton != null)
                {
                    _confirmDeleteButton.onClick.RemoveAllListeners();
                    _confirmDeleteButton.onClick.AddListener(ConfirmDelete);
                }
                
                if (_closePopupButton != null)
                {
                    _closePopupButton.onClick.RemoveAllListeners();
                    _closePopupButton.onClick.AddListener(HidePopup);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при настройке кнопок: {ex.Message}");
            }
        }

        private void SetupToggles()
        {
            try
            {
                if (_showPasswordToggle != null)
                {
                    _showPasswordToggle.onValueChanged.RemoveAllListeners();
                    _showPasswordToggle.isOn = false;
                    _showPasswordToggle.onValueChanged.AddListener(OnToggleShowPassword);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при настройке переключателей: {ex.Message}");
            }
        }

        private void SetupInputFields()
        {
            try
            {
                if (_passwordInput != null)
                {
                    _passwordInput.onValueChanged.RemoveAllListeners();
                    _passwordInput.onValueChanged.AddListener(OnPasswordChanged);
                    _passwordInput.contentType = TMP_InputField.ContentType.Password;
                    _passwordInput.ForceLabelUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при настройке полей ввода: {ex.Message}");
            }
        }
        #endregion
        #endregion

        #region UI Event Handlers
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
            if (_passwordInput == null) 
            {
                Debug.LogError("[RefreshPasswordField] _passwordInput is NULL!");
                yield break;
            }
            if (_passwordInput.fontAsset == null)
            {
                Debug.LogError("[RefreshPasswordField] _passwordInput.fontAsset is NULL! Это вызовет ошибку.");
            }
            else
            {
                Debug.Log($"[RefreshPasswordField] _passwordInput.fontAsset: {_passwordInput.fontAsset.name}");
            }

            _passwordInput.text = "";
            _passwordInput.ForceLabelUpdate();
            yield return new WaitForEndOfFrame();
            _passwordInput.text = _plainPassword;
            _passwordInput.ForceLabelUpdate();
            _passwordInput.caretPosition = _plainPassword.Length;
            _passwordInput.ActivateInputField();
        }
        #endregion

        #region Account Actions
        private void Logout()
        {
            Debug.Log("🔘 Logout нажата");

            // Проверяем, что _auth инициализирован
            if (_auth == null)
            {
                Debug.LogError("❌ _auth is NULL в Logout! Возможно, не успел инициализироваться после перехода на Addressables.");
                ShowPopup("Ошибка выхода из системы. Пожалуйста, попробуйте ещё раз.");
                return;
            }

            // Устанавливаем флаг явного выхода из системы
            SecurePlayerPrefs.SetBool("explicit_logout", true);
            SecurePlayerPrefs.Save();
            Debug.Log("✅ Установлен флаг явного выхода из системы");

            _auth.SignOut();
            ShowPopup("Вы вышли из аккаунта.");

            _sceneSwitcher.ProcessSwitchSceneFor(
                new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        private void ShowDeleteConfirmation()
        {
            Debug.Log("🔘 Показать подтверждение удаления");
            
            // Проверяем, что панель существует
            if (_confirmDeletePanel == null)
            {
                Debug.LogError("❌ _confirmDeletePanel is NULL в ShowDeleteConfirmation! Возможно, после перехода на Addressables префаб не загружен.");
                ShowPopup("Произошла ошибка. Пожалуйста, попробуйте позже.");
                return;
            }
            
            // Проверяем текущее состояние аутентификации
            if (_authStateService == null || !_authStateService.IsAuthenticated)
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
            
            // Используем вспомогательный класс для удаления аккаунта
            _deletionHelper.DeleteAccount(_plainPassword);
        }
        #endregion

        #region UI Messages
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
        #endregion
    }
}
