using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Firebase.Database;
using Newtonsoft.Json;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для работы с профилями пользователей в Firebase Database
    /// </summary>
    public class UserProfileDatabaseService : FirebaseDatabaseServiceBase, IUserProfileDatabaseService
    {
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса профиля пользователя
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public UserProfileDatabaseService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null) 
            : base(database, cacheManager, validationService)
        {
            MyLogger.Log("✅ UserProfileDatabaseService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region IUserProfileDatabaseService Implementation
        /// <summary>
        /// Создает профиль пользователя
        /// </summary>
        public async Task CreateUserProfile(UserProfile profile, string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для создания профиля", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(profile);
                
                await _database.Child("users").Child(targetUserId).Child("profile")
                    .SetRawJsonValueAsync(json);
                
                MyLogger.Log($"Профиль пользователя {targetUserId} создан", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка создания профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Получает профиль пользователя
        /// </summary>
        public async Task<UserProfile> GetUserProfile(string userId = null)
        {
            string targetUserId = userId ?? _userId; // Используем переданный ID или ID текущего пользователя

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("⚠️ ID пользователя не указан для получения профиля", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(targetUserId).Child("profile").GetValueAsync();

                if (snapshot.Exists && snapshot.Value != null)
                {
                    // Используем Newtonsoft.Json для десериализации из словаря/JSON
                    var json = JsonConvert.SerializeObject(snapshot.Value);
                    return JsonConvert.DeserializeObject<UserProfile>(json);
                }

                MyLogger.LogWarning($"Профиль для пользователя {targetUserId} не найден.", MyLogger.LogCategory.Firebase);
                return null;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения профиля (ID: {targetUserId}, MyLogger.LogCategory.Firebase): {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Обновляет профиль пользователя
        /// </summary>
        public async Task UpdateUserProfile(UserProfile profile, string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для обновления профиля", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(profile);
                var updates = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(json);
                
                await _database.Child("users").Child(targetUserId).Child("profile")
                    .UpdateChildrenAsync(updates);
                
                MyLogger.Log($"Профиль пользователя {targetUserId} обновлен", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Обновляет поле профиля пользователя
        /// </summary>
        public async Task UpdateUserProfileField(string field, object value, string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для обновления поля профиля", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(field))
                {
                    throw new ArgumentException("Поле не может быть пустым", nameof(field));
                }
                
                await _database.Child("users").Child(targetUserId).Child("profile").Child(field)
                    .SetValueAsync(value);
                
                MyLogger.Log($"Поле {field} профиля пользователя {targetUserId} обновлено", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления поля профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Проверяет существование профиля пользователя
        /// </summary>
        public async Task<bool> UserProfileExists(string userId = null)
        {
            string targetUserId = userId ?? _userId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                MyLogger.LogWarning("ID пользователя не указан для проверки существования профиля", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(targetUserId).Child("profile").GetValueAsync();
                return snapshot.Exists;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки существования профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Проверяет существование никнейма
        /// </summary>
        public async Task<bool> NicknameExists(string nickname)
        {
            try
            {
                if (string.IsNullOrEmpty(nickname))
                {
                    throw new ArgumentException("Никнейм не может быть пустым", nameof(nickname));
                }
                
                var query = _database.Child("users").OrderByChild("profile/nickname").EqualTo(nickname).LimitToFirst(1);
                var snapshot = await query.GetValueAsync();
                
                return snapshot.Exists && snapshot.ChildrenCount > 0;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки существования никнейма: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Проверяет доступность никнейма
        /// </summary>
        public async Task<(bool available, string error)> CheckNicknameAvailability(string nickname)
        {
            try
            {
                if (string.IsNullOrEmpty(nickname))
                {
                    return (false, "Никнейм не может быть пустым");
                }
                
                if (nickname.Length < 3)
                {
                    return (false, "Никнейм должен содержать не менее 3 символов");
                }
                
                if (nickname.Length > 20)
                {
                    return (false, "Никнейм не должен превышать 20 символов");
                }
                
                if (!Regex.IsMatch(nickname, "^[a-zA-Z0-9_]+$"))
                {
                    return (false, "Никнейм может содержать только латинские буквы, цифры и символ подчеркивания");
                }
                
                bool exists = await NicknameExists(nickname);
                
                return exists ? 
                    (false, "Этот никнейм уже занят") : 
                    (true, null);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка проверки доступности никнейма: {ex.Message}", MyLogger.LogCategory.Firebase);
                return (false, "Произошла ошибка при проверке никнейма");
            }
        }
        #endregion
    }
} 