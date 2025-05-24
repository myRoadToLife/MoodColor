using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using Firebase.Auth;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Auth.Services
{
    /// <summary>
    /// Сервис для управления состоянием аутентификации Firebase.
    /// Отслеживает изменения состояния и обеспечивает восстановление аутентификации.
    /// </summary>
    public class AuthStateService : IAuthStateService
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
            MyLogger.Log("✅ AuthStateService инициализирован", MyLogger.LogCategory.Firebase);
        }
        
        /// <summary>
        /// Начинает отслеживать изменения состояния аутентификации
        /// </summary>
        private void StartListeningAuthState()
        {
            if (_isListeningAuthState) return;
            
            _auth.StateChanged += HandleAuthStateChanged;
            _isListeningAuthState = true;
            MyLogger.Log("✅ Начато отслеживание состояния аутентификации", MyLogger.LogCategory.Firebase);
        }
        
        /// <summary>
        /// Обработчик изменения состояния аутентификации
        /// </summary>
        private void HandleAuthStateChanged(object sender, EventArgs args)
        {
            FirebaseUser user = _auth.CurrentUser;
            
            if (user == null)
            {
                MyLogger.Log("🔄 Состояние аутентификации изменилось: пользователь не авторизован", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.Log($"🔄 Состояние аутентификации изменилось: пользователь {user.Email} авторизован", MyLogger.LogCategory.Firebase);
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
            MyLogger.Log("✅ Остановлено отслеживание состояния аутентификации", MyLogger.LogCategory.Firebase);
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
                    MyLogger.Log($"✅ Пользователь уже авторизован: {CurrentUser.Email}", MyLogger.LogCategory.Firebase);
                    return true;
                }
                
                // Проверяем, есть ли сохраненные учетные данные
                bool rememberMe = SecurePlayerPrefs.GetBool("remember_me", false);
                string savedEmail = SecurePlayerPrefs.GetString("email", "");
                string savedPassword = SecurePlayerPrefs.GetString("password", "");
                
                if (!rememberMe || string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword))
                {
                    MyLogger.Log("⚠️ Нет сохраненных учетных данных для восстановления аутентификации", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                MyLogger.Log($"🔄 Восстановление аутентификации для: {savedEmail}", MyLogger.LogCategory.Firebase);
                
                // Выполняем вход с сохраненными учетными данными
                var result = await _authService.LoginUser(savedEmail, savedPassword);
                
                if (result.success)
                {
                    MyLogger.Log("✅ Аутентификация успешно восстановлена", MyLogger.LogCategory.Firebase);
                    return true;
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ Не удалось восстановить аутентификацию: {result.error}", MyLogger.LogCategory.Firebase);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при восстановлении аутентификации: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                    MyLogger.LogWarning("⚠️ Нет текущего пользователя для обновления", MyLogger.LogCategory.Firebase);
                    return false;
                }
                
                await CurrentUser.ReloadAsync();
                MyLogger.Log($"✅ Информация о пользователе {CurrentUser.Email} обновлена", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
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