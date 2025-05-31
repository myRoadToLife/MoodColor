// UserProfileService.cs (полный код)
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using App.Develop.Utils.Logging;
using Firebase.Auth;
// Добавляем алиасы для разрешения конфликта имен
using DatabaseUserProfile = App.Develop.CommonServices.Firebase.Database.Models.UserProfile;
using AuthUserProfile = Firebase.Auth.UserProfile;

namespace App.Develop.CommonServices.Firebase.Auth.Services
{
    /// <summary>
    /// Интерфейс сервиса для управления профилем пользователя
    /// </summary>
    public interface IUserProfileService
    {
        Task<bool> UpdateDisplayName(string displayName);
        Task<bool> UpdatePhotoUrl(string photoUrl);
        Task<bool> UpdateEmail(string newEmail);
        Task<DatabaseUserProfile> GetCurrentUserProfile();
        Task<bool> SetupProfile(string nickname, string gender);
        Task<bool> UpdateLastActive();
        Task<bool> UpdateUserSettings(bool notifications, string theme, bool sound);
        Task<bool> UpdateUserData(string userId, Dictionary<string, object> userData);
    }
    
    /// <summary>
    /// Сервис для управления профилем пользователя
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        #region Dependencies
        private readonly FirebaseAuth _auth;
        private readonly IDatabaseService _databaseService;
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса управления профилем пользователя
        /// </summary>
        /// <param name="databaseService">Сервис базы данных</param>
        public UserProfileService(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _auth = FirebaseAuth.DefaultInstance;
            
            // Подписываемся на изменение состояния аутентификации
            _auth.StateChanged += AuthStateChanged;
            
            MyLogger.Log("✅ UserProfileService создан", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region IUserProfileService Implementation
        /// <summary>
        /// Обновляет отображаемое имя пользователя
        /// </summary>
        /// <param name="displayName">Новое отображаемое имя</param>
        public async Task<bool> UpdateDisplayName(string displayName)
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                var user = _auth.CurrentUser;
                if (user == null)
                {
                    MyLogger.LogWarning("⚠️ Попытка обновления имени без авторизации", MyLogger.LogCategory.Firebase);
                    return false;
                }

                // Обновляем имя в Firebase Auth
                var profileUpdates = user.UpdateUserProfileAsync(new AuthUserProfile
                {
                    DisplayName = displayName,
                    PhotoUrl = user.PhotoUrl
                });
                await profileUpdates;
                
                // Обновляем данные в базе данных
                await _databaseService.UpdateUserProfileField("nickname", displayName);
                
                MyLogger.Log($"✅ Имя пользователя обновлено: {displayName}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении имени пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Обновляет URL фотографии профиля
        /// </summary>
        /// <param name="photoUrl">Новый URL фотографии</param>
        public async Task<bool> UpdatePhotoUrl(string photoUrl)
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                var user = _auth.CurrentUser;
                if (user == null)
                {
                    MyLogger.LogWarning("⚠️ Попытка обновления фото профиля без авторизации", MyLogger.LogCategory.Firebase);
                    return false;
                }

                // Обновляем URL фото в Firebase Auth
                var profileUpdates = user.UpdateUserProfileAsync(new AuthUserProfile
                {
                    DisplayName = user.DisplayName,
                    PhotoUrl = new Uri(photoUrl)
                });
                await profileUpdates;
                
                // Обновляем данные в базе данных
                await _databaseService.UpdateUserProfileField("photoUrl", photoUrl);
                
                MyLogger.Log($"✅ Фото профиля пользователя обновлено: {photoUrl}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении фото профиля пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Обновляет email пользователя
        /// </summary>
        /// <param name="newEmail">Новый email</param>
        public async Task<bool> UpdateEmail(string newEmail)
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                var user = _auth.CurrentUser;
                if (user == null)
                {
                    MyLogger.LogWarning("⚠️ Попытка обновления email без авторизации", MyLogger.LogCategory.Firebase);
                    return false;
                }

                // Обновляем email
                await user.UpdateEmailAsync(newEmail);
                
                // Обновляем данные в базе данных
                await _databaseService.UpdateUserProfileField("email", newEmail);
                
                MyLogger.Log($"✅ Email пользователя обновлен: {newEmail}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении email пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Получает текущий профиль пользователя
        /// </summary>
        public async Task<DatabaseUserProfile> GetCurrentUserProfile()
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                var user = _auth.CurrentUser;
                if (user == null)
                {
                    MyLogger.LogWarning("⚠️ Попытка получения профиля пользователя без авторизации", MyLogger.LogCategory.Firebase);
                    return null;
                }

                // Получаем данные пользователя из базы данных
                var userProfile = await _databaseService.GetUserProfile(user.UserId);
                
                // Если профиль не найден, создаем новый на основе данных аутентификации
                if (userProfile == null)
                {
                    userProfile = new DatabaseUserProfile
                    {
                        Email = user.Email,
                        Nickname = user.DisplayName ?? "User",
                        PhotoUrl = user.PhotoUrl?.ToString(),
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        LastActive = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        IsOnline = true
                    };
                    
                    // Сохраняем новый профиль в базу данных
                    await _databaseService.CreateUserProfile(userProfile);
                }
                
                return userProfile;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при получении профиля пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Настраивает профиль пользователя
        /// </summary>
        /// <param name="nickname">Никнейм пользователя</param>
        /// <param name="gender">Пол пользователя</param>
        public async Task<bool> SetupProfile(string nickname, string gender)
        {
            try
            {
                if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(gender))
                {
                    MyLogger.LogWarning("⚠️ Никнейм и пол не могут быть пустыми", MyLogger.LogCategory.Firebase);
                    return false;
                }

                await _databaseService.UpdateUserProfileField("nickname", nickname);
                await _databaseService.UpdateUserProfileField("gender", gender);
                await _databaseService.UpdateUserProfileField("lastActive", ServerValue.Timestamp);
                
                MyLogger.Log($"✅ Профиль пользователя настроен. Никнейм: {nickname}, Пол: {gender}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при настройке профиля пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        public async Task<bool> UpdateLastActive()
        {
            try
            {
                await _databaseService.UpdateUserProfileField("lastActive", ServerValue.Timestamp);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении lastActive: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        public async Task<bool> UpdateUserSettings(bool notifications, string theme, bool sound)
        {
            try
            {
                await _databaseService.UpdateUserProfileField("settings/notifications", notifications);
                await _databaseService.UpdateUserProfileField("settings/theme", theme);
                await _databaseService.UpdateUserProfileField("settings/sound", sound);
                
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении настроек: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Обновляет данные пользователя в базе данных
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="userData">Словарь с данными для обновления</param>
        public async Task<bool> UpdateUserData(string userId, Dictionary<string, object> userData)
        {
            try
            {
                // Проверяем данные пользователя
                if (string.IsNullOrEmpty(userId))
                {
                    MyLogger.LogError("❌ ID пользователя не может быть пустым", MyLogger.LogCategory.Firebase);
                    return false;
                }

                if (userData == null || userData.Count == 0)
                {
                    MyLogger.LogWarning("⚠️ Нет данных для обновления", MyLogger.LogCategory.Firebase);
                    return false;
                }

                // Обновляем каждое поле отдельно
                foreach (var kvp in userData)
                {
                    await _databaseService.UpdateUserProfileField(kvp.Key, kvp.Value, userId);
                }

                MyLogger.Log($"✅ Данные пользователя обновлены. UserId: {userId}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении данных пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        // Обработчик изменения состояния аутентификации
        private void AuthStateChanged(object sender, EventArgs e)
        {
            var user = _auth.CurrentUser;
            if (user != null)
            {
                // Пользователь вошел в систему
                _databaseService.UpdateUserId(user.UserId);
                MyLogger.Log($"👤 Состояние аутентификации изменено: пользователь {user.UserId} вошел в систему", MyLogger.LogCategory.Firebase);
            }
            else
            {
                // Пользователь вышел из системы
                _databaseService.UpdateUserId(null);
                MyLogger.Log("🚶‍♂️ Состояние аутентификации изменено: пользователь вышел из системы", MyLogger.LogCategory.Firebase);
            }
        }
    }
}