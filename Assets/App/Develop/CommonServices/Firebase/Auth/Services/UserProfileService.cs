// UserProfileService.cs (полный код)
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Services;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Auth.Services
{
    public class UserProfileService
    {
        private readonly DatabaseService _databaseService;

        public UserProfileService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public async Task<bool> SetupProfile(string nickname, string gender)
        {
            try
            {
                if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(gender))
                {
                    MyLogger.LogWarning("⚠️ Никнейм и пол не могут быть пустыми", MyLogger.LogCategory.Firebase);
                    return false;
                }

                var updates = new Dictionary<string, object>
                {
                    ["profile/nickname"] = nickname,
                    ["profile/gender"] = gender,
                    ["profile/lastActive"] = ServerValue.Timestamp
                };

                await _databaseService.UpdateUserData(updates);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при настройке профиля: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        public async Task<bool> UpdateLastActive()
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    ["profile/lastActive"] = ServerValue.Timestamp
                };

                await _databaseService.UpdateUserData(updates);
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
                var updates = new Dictionary<string, object>
                {
                    ["profile/settings/notifications"] = notifications,
                    ["profile/settings/theme"] = theme,
                    ["profile/settings/sound"] = sound
                };

                await _databaseService.UpdateUserData(updates);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении настроек: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
    }
}