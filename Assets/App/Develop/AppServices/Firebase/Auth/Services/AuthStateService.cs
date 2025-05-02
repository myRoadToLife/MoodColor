using System;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using Firebase.Auth;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth.Services
{
    /// <summary>
    /// Сервис для управления состоянием аутентификации Firebase.
    /// Отслеживает изменения состояния и обеспечивает восстановление аутентификации.
    /// </summary>
    public class AuthStateService
    {
        private readonly FirebaseAuth _auth;
        private readonly IAuthService _authService;
        
        private bool _isInitialized = false;
        private bool _isListeningAuthState = false;
        
        // Событие изменения состояния аутентификации
        public event Action<FirebaseUser> AuthStateChanged;
        
        // Текущий пользователь
        public FirebaseUser CurrentUser => _auth?.CurrentUser;
        
        // Авторизован ли пользователь
        public bool IsAuthenticated => CurrentUser != null;

        public AuthStateService(FirebaseAuth auth, IAuthService authService)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            
            Initialize();
        }
        
        /// <summary>
        /// Инициализирует сервис и начинает отслеживать состояние аутентификации
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;
            
            StartListeningAuthState();
            _isInitialized = true;
            Debug.Log("✅ AuthStateService инициализирован");
        }
        
        /// <summary>
        /// Начинает отслеживать изменения состояния аутентификации
        /// </summary>
        private void StartListeningAuthState()
        {
            if (_isListeningAuthState) return;
            
            _auth.StateChanged += HandleAuthStateChanged;
            _isListeningAuthState = true;
            Debug.Log("✅ Начато отслеживание состояния аутентификации");
        }
        
        /// <summary>
        /// Обработчик изменения состояния аутентификации
        /// </summary>
        private void HandleAuthStateChanged(object sender, EventArgs args)
        {
            FirebaseUser user = _auth.CurrentUser;
            
            if (user == null)
            {
                Debug.Log("🔄 Состояние аутентификации изменилось: пользователь не авторизован");
            }
            else
            {
                Debug.Log($"🔄 Состояние аутентификации изменилось: пользователь {user.Email} авторизован");
            }
            
            // Уведомляем подписчиков о смене состояния
            AuthStateChanged?.Invoke(user);
        }
        
        /// <summary>
        /// Останавливает отслеживание изменений состояния аутентификации
        /// </summary>
        public void StopListeningAuthState()
        {
            if (!_isListeningAuthState) return;
            
            _auth.StateChanged -= HandleAuthStateChanged;
            _isListeningAuthState = false;
            Debug.Log("✅ Остановлено отслеживание состояния аутентификации");
        }
        
        /// <summary>
        /// Восстанавливает аутентификацию, используя сохраненные учетные данные
        /// </summary>
        public async Task<bool> RestoreAuthenticationAsync()
        {
            try
            {
                // Проверяем, есть ли уже авторизованный пользователь
                if (IsAuthenticated)
                {
                    Debug.Log($"✅ Пользователь уже авторизован: {CurrentUser.Email}");
                    return true;
                }
                
                // Проверяем, есть ли сохраненные учетные данные
                bool rememberMe = SecurePlayerPrefs.GetBool("remember_me", false);
                string savedEmail = SecurePlayerPrefs.GetString("email", "");
                string savedPassword = SecurePlayerPrefs.GetString("password", "");
                
                if (!rememberMe || string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword))
                {
                    Debug.Log("⚠️ Нет сохраненных учетных данных для восстановления аутентификации");
                    return false;
                }
                
                Debug.Log($"🔄 Восстановление аутентификации для: {savedEmail}");
                
                // Выполняем вход с сохраненными учетными данными
                var result = await _authService.LoginUser(savedEmail, savedPassword);
                
                if (result.success)
                {
                    Debug.Log("✅ Аутентификация успешно восстановлена");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Не удалось восстановить аутентификацию: {result.error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при восстановлении аутентификации: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет состояние текущего пользователя и обновляет его информацию
        /// </summary>
        public async Task<bool> RefreshUserAsync()
        {
            try
            {
                if (CurrentUser == null)
                {
                    Debug.LogWarning("⚠️ Нет текущего пользователя для обновления");
                    return false;
                }
                
                await CurrentUser.ReloadAsync();
                Debug.Log($"✅ Информация о пользователе {CurrentUser.Email} обновлена");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при обновлении пользователя: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Деструктор для очистки ресурсов
        /// </summary>
        ~AuthStateService()
        {
            StopListeningAuthState();
        }
    }
} 