using System.Collections;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.Emotion;

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
        private EmotionService _emotionService;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            MyLogger.Log("✅ AccountDeletionManager.Start вызван. В случае ошибок, убедитесь, что инъекция произошла до Start.");
            
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
                MyLogger.LogError($"❌ _authStateService остался NULL после {waitTime} секунд ожидания! " +
                              "Проблема с инъекцией зависимостей при использовании Addressables.");
                // Возможно, стоит показать пользователю сообщение об ошибке или перезагрузить сцену
                ShowPopup("Произошла ошибка при загрузке. Пожалуйста, попробуйте позже.");
                yield break;
            }
            
            // Теперь, когда у нас есть все зависимости, можно инициализировать компонент
            MyLogger.Log($"✅ _authStateService успешно получен после {waitTime} секунд ожидания.");
            
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
            MyLogger.Log("🚀 [AccountDeletionManager] Inject CALLED. Container is null: " + (container == null));

            try 
            {
                if (container == null) 
                {
                    MyLogger.LogError("❌ [AccountDeletionManager] Inject CALLED with a NULL container!");
                    throw new ArgumentNullException(nameof(container));
                }
                
                _sceneSwitcher = container.Resolve<SceneSwitcher>();
                if (_sceneSwitcher == null) MyLogger.LogError("❌ [AccountDeletionManager] Не удалось получить SceneSwitcher из контейнера!");
                
                _auth = FirebaseAuth.DefaultInstance;
                if (_auth == null) MyLogger.LogError("❌ [AccountDeletionManager] FirebaseAuth.DefaultInstance вернул NULL!");
                
                _database = container.Resolve<DatabaseReference>();
                if (_database == null) MyLogger.LogError("❌ [AccountDeletionManager] Не удалось получить DatabaseReference из контейнера!");
                
                // Пытаемся получить сервис и логируем результат
                IAuthStateService resolvedService = null;
                try
                {
                    resolvedService = container.Resolve<IAuthStateService>();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ [AccountDeletionManager] ОШИБКА при попытке Resolve<IAuthStateService>: {ex.Message}\n{ex.StackTrace}");
                }
                
                _authStateService = resolvedService;
                if (_authStateService == null) 
                {
                    MyLogger.LogError("❌ [AccountDeletionManager] IAuthStateService остался NULL после попытки Resolve! Это критическая ошибка.");
                    return; 
                }
                else
                {
                    MyLogger.Log("✅ [AccountDeletionManager] IAuthStateService УСПЕШНО получен из контейнера.");
                }
                
                _deletionHelper = new AccountDeletionHelper(_database, _authStateService);
                _emotionService = container.Resolve<EmotionService>();
                if (_emotionService == null) MyLogger.LogError("❌ [AccountDeletionManager] Не удалось получить EmotionService из контейнера!");
                SubscribeToHelperEvents();

                InitializeUI();
                MyLogger.Log("✅ AccountDeletionManager успешно инициализирован через Inject");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Глобальная ошибка в Inject AccountDeletionManager: {ex.Message}\n{ex.StackTrace}");
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
                _emotionService?.ClearHistory();
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
                MyLogger.LogWarning("⚠️ Пользователь вышел из системы или сессия истекла");
                ShowPopup("Ваша сессия истекла. Пожалуйста, войдите снова.");
                
                // Задержка перед редиректом, чтобы пользователь успел прочитать сообщение
                StartCoroutine(DelayedRedirect());
            }
            else
            {
                MyLogger.Log($"✅ Состояние аутентификации обновлено: {user.Email}");
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
                MyLogger.LogError("❌ _authStateService is NULL в CheckAuthenticationState!");
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
                MyLogger.LogError("❌ _authStateService is NULL в CheckAndRestoreAuthenticationState! Инъекция не произошла или запущена слишком поздно.");
                yield break;
            }

            // Проверяем, авторизован ли пользователь
            if (!_authStateService.IsAuthenticated)
            {
                MyLogger.LogWarning("⚠️ Пользователь не авторизован при открытии AccountDeletionManager");
                
                // Пытаемся восстановить аутентификацию
                var restoreTask = _authStateService.RestoreAuthenticationAsync();
                
                // Ждем завершения восстановления
                while (!restoreTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (restoreTask.Result)
                {
                    MyLogger.Log("✅ Аутентификация успешно восстановлена");
                    // Обновляем ссылку наAuth
                    _auth = FirebaseAuth.DefaultInstance;
                }
                else
                {
                    MyLogger.LogError("❌ Не удалось восстановить аутентификацию");
                    ShowPopup("Не удалось восстановить сессию. Пожалуйста, войдите снова.");
                    StartCoroutine(DelayedRedirect());
                }
            }
            else
            {
                MyLogger.Log($"✅ Пользователь авторизован: {_authStateService.CurrentUser.Email}");
                // Обновляем информацию о пользователе
                yield return StartCoroutine(RefreshUserCoroutine());
            }
        }
        
        private IEnumerator RefreshUserCoroutine()
        {
            if (_authStateService != null && _authStateService.IsAuthenticated)
            {
                var reloadTask = _authStateService.CurrentUser.ReloadAsync();
                yield return new WaitUntil(() => reloadTask.IsCompleted);
                
                if (reloadTask.IsFaulted)
                {
                    MyLogger.LogWarning("⚠️ Не удалось обновить информацию о пользователе");
                }
            }
        }
        #endregion

        #region UI Initialization
        private void InitializeUI()
        {
            MyLogger.Log("✅ InitializeUI вызван");
            bool hasErrors = false;

            // Проверка основных элементов
            if (_logoutButton == null) { MyLogger.LogError("❌ _logoutButton не установлен!"); hasErrors = true; }
            if (_showDeleteButton == null) { MyLogger.LogError("❌ _showDeleteButton не установлен!"); hasErrors = true; }
            if (_confirmDeleteButton == null) { MyLogger.LogError("❌ _confirmDeleteButton не установлен!"); hasErrors = true; }
            if (_cancelDeleteButton == null) { MyLogger.LogError("❌ _cancelDeleteButton не установлен!"); hasErrors = true; }
            if (_passwordInput == null) { MyLogger.LogError("❌ _passwordInput не установлен!"); hasErrors = true; }
            if (_showPasswordToggle == null) { MyLogger.LogError("❌ _showPasswordToggle не установлен!"); hasErrors = true; }
            if (_popupPanel == null) { MyLogger.LogError("❌ _popupPanel не установлен!"); hasErrors = true; }
            if (_popupText == null) { MyLogger.LogError("❌ _popupText не установлен!"); hasErrors = true; }
            
            // Необязательный элемент
            if (_closePopupButton == null)
            {
                MyLogger.LogWarning("⚠️ _closePopupButton не установлен, всплывающие сообщения будут закрываться автоматически");
            }
            
            if (hasErrors)
            {
                MyLogger.LogError("❌ Критические UI элементы отсутствуют. Возможно, префаб не загружен полностью через Addressables.");
                return;
            }

            try
            {
                SetupButtons();
                SetupToggles();
                SetupInputFields();
                SetPasswordVisibility(false);
                _confirmDeletePanel.SetActive(false);
                MyLogger.Log("✅ Интерфейс успешно инициализирован");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при инициализации UI: {ex.Message}\n{ex.StackTrace}");
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
                MyLogger.LogError($"❌ Ошибка при настройке кнопок: {ex.Message}");
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
                MyLogger.LogError($"❌ Ошибка при настройке переключателей: {ex.Message}");
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
                MyLogger.LogError($"❌ Ошибка при настройке полей ввода: {ex.Message}");
            }
        }
        #endregion
        #endregion

        #region UI Event Handlers
        private void OnPasswordChanged(string newText)
        {
            _plainPassword = newText;
            MyLogger.Log($"⌨ Введённый пароль изменён: {newText}");
        }

        private void OnToggleShowPassword(bool isVisible)
        {
            MyLogger.Log($"🔁 Toggle пароль: {(isVisible ? "Показать" : "Скрыть")}");
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
                MyLogger.LogError("[RefreshPasswordField] _passwordInput is NULL!");
                yield break;
            }
            
            // Проверяем, есть ли необходимые компоненты TextMeshPro
            if (_passwordInput.textComponent == null)
            {
                MyLogger.LogError("[RefreshPasswordField] _passwordInput.textComponent is NULL! Это критическая проблема для TMP_InputField.");
                yield break;
            }
            
            if (_passwordInput.fontAsset == null)
            {
                MyLogger.LogError("[RefreshPasswordField] _passwordInput.fontAsset is NULL! Это вызовет ошибку.");
                // Fallback на системный шрифт или ничего не делать, зависит от требований
            }
            else
            {
                MyLogger.Log($"[RefreshPasswordField] _passwordInput.fontAsset: {_passwordInput.fontAsset.name}");
            }

            // Очищаем текст и ждем фрейм для обновления
            _passwordInput.text = "";
            _passwordInput.ForceLabelUpdate();
            yield return new WaitForEndOfFrame();
            
            // Устанавливаем новый текст и ждем еще один фрейм для обновления
            _passwordInput.text = _plainPassword ?? "";
            _passwordInput.ForceLabelUpdate();
            yield return new WaitForEndOfFrame();
            
            // Безопасная установка позиции курсора с проверками
            try
            {
                int caretPos = _plainPassword != null ? _plainPassword.Length : 0;
                if (_passwordInput.textComponent != null && _passwordInput.textComponent.textInfo != null)
                {
                    // Устанавливаем позицию курсора только если компоненты текста готовы
                    _passwordInput.caretPosition = caretPos;
                    _passwordInput.ActivateInputField();
                }
                else
                {
                    MyLogger.LogWarning("[RefreshPasswordField] Не удалось установить позицию курсора - компоненты текста не готовы.");
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[RefreshPasswordField] Ошибка при установке курсора: {ex.Message}");
            }
        }
        #endregion

        #region Account Actions
        private void Logout()
        {
            MyLogger.Log("🔘 Logout нажата");
            
            if (_auth == null)
            {
                MyLogger.LogError("❌ _auth is NULL в Logout! Возможно, не успел инициализироваться после перехода на Addressables.");
                // Попытка получить экземпляр снова, если это возможно
                _auth = FirebaseAuth.DefaultInstance;
                if (_auth == null)
                {
                    ShowPopup("Ошибка выхода: сервис аутентификации недоступен.");
                    return;
                }
            }
            
            _auth.SignOut();
            SecurePlayerPrefs.SetBool("explicit_logout", true);
            SecurePlayerPrefs.Save();
            MyLogger.Log("✅ Установлен флаг явного выхода из системы");

            _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        private void ShowDeleteConfirmation()
        {
            MyLogger.Log("🔘 Показать подтверждение удаления");
            
            if (_confirmDeletePanel == null)
            {
                MyLogger.LogError("❌ _confirmDeletePanel is NULL в ShowDeleteConfirmation! Возможно, после перехода на Addressables префаб не загружен.");
                return;
            }
            
            // Дополнительная проверка аутентификации перед показом панели
            if (_authStateService == null || !_authStateService.IsAuthenticated)
            {
                MyLogger.LogError("❌ Пользователь не авторизован при открытии панели удаления");
                ShowPopup("Для удаления аккаунта необходимо войти в систему.");
                StartCoroutine(DelayedRedirect());
                return;
            }
            
            _confirmDeletePanel.SetActive(true);
            _passwordInput.text = "";
            SetPasswordVisibility(false); // По умолчанию пароль скрыт
        }

        private void CancelDelete()
        {
            MyLogger.Log("🔘 Отмена удаления");
            _confirmDeletePanel.SetActive(false);
        }

        private void ConfirmDelete()
        {
            MyLogger.Log("🔘 Подтвердить удаление");
            
            if (_deletionHelper != null)
            {
                _deletionHelper.DeleteAccount(_plainPassword);
            }
        }
        #endregion

        #region UI Messages
        private void ShowPopup(string message)
        {
            MyLogger.Log($"📢 Показ сообщения: {message}");
            if (_popupPanel != null && _popupText != null)
            {
                _popupText.text = message;
                _popupPanel.SetActive(true);

                // Если кнопка закрытия есть, то не скрываем автоматически
                if (_closePopupButton == null)
                {
                    StartCoroutine(HidePopupAfterDelay(3f));
                }
            }
        }

        private void HidePopup()
        {
            MyLogger.Log("🔍 Скрытие всплывающего сообщения");
            if (_popupPanel != null)
            {
                _popupPanel.SetActive(false);
            }
        }

        private IEnumerator HidePopupAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HidePopup();
        }
        #endregion
    }
}
