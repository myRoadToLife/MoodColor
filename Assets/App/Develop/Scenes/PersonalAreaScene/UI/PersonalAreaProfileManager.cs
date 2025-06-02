using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Управляет загрузкой и отображением профиля пользователя
    /// Отвечает только за работу с профилем пользователя
    /// </summary>
    public class PersonalAreaProfileManager
    {
        private const string DEFAULT_USERNAME = "Username";
        
        private readonly IPersonalAreaView _view;
        private readonly IDatabaseService _databaseService;

        public PersonalAreaProfileManager(IPersonalAreaView view, IDatabaseService databaseService = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _databaseService = databaseService; // Может быть null, если сервис недоступен
        }

        /// <summary>
        /// Инициализирует профиль пользователя
        /// </summary>
        public async Task InitializeAsync()
        {
            // Устанавливаем дефолтные значения
            _view.SetUsername(DEFAULT_USERNAME);
            _view.SetCurrentEmotion(null);
            
            // Асинхронно загружаем реальный профиль
            await LoadUserProfileAsync();
        }

        /// <summary>
        /// Загружает профиль пользователя из базы данных
        /// </summary>
        private async Task LoadUserProfileAsync()
        {
            if (_databaseService == null)
            {
                MyLogger.Log("DatabaseService недоступен, используется дефолтный профиль", MyLogger.LogCategory.UI);
                return;
            }
            
            try
            {
                // Проверяем авторизацию
                if (!_databaseService.IsAuthenticated)
                {
                    MyLogger.Log("Пользователь не авторизован, используется дефолтный профиль", MyLogger.LogCategory.UI);
                    return;
                }
                
                // Загружаем профиль
                UserProfile userProfile = await _databaseService.GetUserProfile();
                
                if (userProfile != null && !string.IsNullOrEmpty(userProfile.Nickname))
                {
                    _view.SetUsername(userProfile.Nickname);
                    MyLogger.Log($"Профиль пользователя загружен: {userProfile.Nickname}", MyLogger.LogCategory.UI);
                }
                else
                {
                    MyLogger.LogWarning("Профиль пользователя пуст или некорректен", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка загрузки профиля пользователя: {ex.Message}", MyLogger.LogCategory.UI);
                // Не бросаем исключение, так как приложение может работать с дефолтным профилем
            }
        }

        /// <summary>
        /// Обновляет имя пользователя
        /// </summary>
        public async Task UpdateUsernameAsync(string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(newUsername));
            }

            try
            {
                _view.SetUsername(newUsername);
                
                if (_databaseService?.IsAuthenticated == true)
                {
                    // Здесь можно добавить логику сохранения в базу данных
                    MyLogger.Log($"Имя пользователя обновлено: {newUsername}", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления имени пользователя: {ex.Message}", MyLogger.LogCategory.UI);
                throw;
            }
        }
    }
} 