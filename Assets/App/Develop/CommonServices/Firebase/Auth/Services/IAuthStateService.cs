using System;
using System.Threading.Tasks;
using Firebase.Auth;

namespace App.Develop.CommonServices.Firebase.Auth.Services
{
    /// <summary>
    /// Интерфейс для сервиса управления состоянием аутентификации
    /// </summary>
    public interface IAuthStateService
    {
        /// <summary>
        /// Событие изменения состояния аутентификации
        /// </summary>
        event Action<FirebaseUser> AuthStateChanged;
        
        /// <summary>
        /// Текущий пользователь Firebase
        /// </summary>
        FirebaseUser CurrentUser { get; }
        
        /// <summary>
        /// Флаг аутентифицирован ли пользователь
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Останавливает отслеживание изменений состояния аутентификации
        /// </summary>
        void StopListeningAuthState();
        
        /// <summary>
        /// Восстанавливает аутентификацию, используя сохраненные учетные данные
        /// </summary>
        Task<bool> RestoreAuthenticationAsync();
        
        /// <summary>
        /// Проверяет состояние текущего пользователя и обновляет его информацию
        /// </summary>
        Task<bool> RefreshUserAsync();
    }
} 