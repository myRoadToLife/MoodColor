using App.Develop.CommonServices.Firebase.Database.Models;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с профилем пользователя в базе данных Firebase
    /// </summary>
    public interface IUserProfileDatabaseService
    {
        /// <summary>
        /// Получает профиль пользователя
        /// </summary>
        Task<UserProfile> GetUserProfile(string userId = null);
        
        /// <summary>
        /// Создает профиль пользователя
        /// </summary>
        Task CreateUserProfile(UserProfile profile, string userId = null);
        
        /// <summary>
        /// Обновляет профиль пользователя
        /// </summary>
        Task UpdateUserProfile(UserProfile profile, string userId = null);
        
        /// <summary>
        /// Обновляет определенное поле профиля пользователя
        /// </summary>
        Task UpdateUserProfileField(string field, object value, string userId = null);
        
        /// <summary>
        /// Проверяет существование профиля пользователя
        /// </summary>
        Task<bool> UserProfileExists(string userId = null);
        
        /// <summary>
        /// Проверяет существование никнейма
        /// </summary>
        Task<bool> NicknameExists(string nickname);
        
        /// <summary>
        /// Проверяет доступность никнейма
        /// </summary>
        Task<(bool available, string error)> CheckNicknameAvailability(string nickname);
    }
} 