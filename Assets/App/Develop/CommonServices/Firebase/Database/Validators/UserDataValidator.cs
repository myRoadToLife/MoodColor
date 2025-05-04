using System;
using System.Text.RegularExpressions;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.CommonServices.Firebase.Database.Validators
{
    /// <summary>
    /// Валидатор данных пользователя
    /// </summary>
    public class UserDataValidator : IDataValidator<UserData>
    {
        private const int MAX_NICKNAME_LENGTH = 30;
        private const int MIN_NICKNAME_LENGTH = 2;
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        
        /// <summary>
        /// Выполняет комплексную валидацию данных пользователя
        /// </summary>
        /// <param name="userData">Данные пользователя для валидации</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(UserData userData)
        {
            var result = new ValidationResult();
            
            if (userData == null)
            {
                result.AddError("userData", "Данные пользователя не могут быть пустыми", "ERR_NULL_USERDATA");
                return result;
            }
            
            // Валидация профиля пользователя
            if (userData.Profile == null)
            {
                result.AddError("profile", "Профиль пользователя не может быть пустым", "ERR_NULL_PROFILE");
            }
            else
            {
                ValidateProfile(userData.Profile, result);
            }
            
            // Валидация текущей эмоции
            if (userData.CurrentEmotion == null)
            {
                result.AddError("currentEmotion", "Текущая эмоция не может быть пустой", "ERR_NULL_CURRENT_EMOTION");
            }
            else
            {
                ValidateCurrentEmotion(userData.CurrentEmotion, result);
            }
            
            // Валидация настроек синхронизации
            if (userData.SyncSettings == null)
            {
                result.AddError("syncSettings", "Настройки синхронизации не могут быть пустыми", "ERR_NULL_SYNC_SETTINGS");
            }
            
            // Проверка коллекций на null
            if (userData.Emotions == null)
            {
                result.AddError("emotions", "Коллекция эмоций не может быть пустой", "ERR_NULL_EMOTIONS");
            }
            
            if (userData.Jars == null)
            {
                result.AddError("jars", "Коллекция банок не может быть пустой", "ERR_NULL_JARS");
            }
            
            if (userData.EmotionHistory == null)
            {
                result.AddError("emotionHistory", "История эмоций не может быть пустой", "ERR_NULL_EMOTION_HISTORY");
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверяет, являются ли данные пользователя валидными
        /// </summary>
        /// <param name="userData">Данные для проверки</param>
        /// <returns>true если данные валидны, иначе false</returns>
        public bool IsValid(UserData userData)
        {
            return Validate(userData).IsValid;
        }
        
        /// <summary>
        /// Выполняет валидацию профиля пользователя
        /// </summary>
        /// <param name="profile">Профиль для валидации</param>
        /// <param name="result">Результат валидации для добавления ошибок</param>
        private void ValidateProfile(UserProfile profile, ValidationResult result)
        {
            // Проверка email
            if (string.IsNullOrEmpty(profile.Email))
            {
                result.AddError("profile.email", "Email не может быть пустым", "ERR_EMPTY_EMAIL");
            }
            else if (!IsValidEmail(profile.Email))
            {
                result.AddError("profile.email", "Недопустимый формат email", "ERR_INVALID_EMAIL");
            }
            
            // Проверка никнейма
            if (string.IsNullOrEmpty(profile.Nickname))
            {
                result.AddError("profile.nickname", "Никнейм не может быть пустым", "ERR_EMPTY_NICKNAME");
            }
            else if (profile.Nickname.Length < MIN_NICKNAME_LENGTH || profile.Nickname.Length > MAX_NICKNAME_LENGTH)
            {
                result.AddError("profile.nickname", 
                    $"Никнейм должен быть от {MIN_NICKNAME_LENGTH} до {MAX_NICKNAME_LENGTH} символов", 
                    "ERR_INVALID_NICKNAME_LENGTH");
            }
            
            // Проверка пола
            if (!string.IsNullOrEmpty(profile.Gender) && 
                !string.Equals(profile.Gender, "male", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(profile.Gender, "female", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(profile.Gender, "other", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("profile.gender", "Недопустимое значение пола", "ERR_INVALID_GENDER");
            }
            
            // Проверка очков
            if (profile.TotalPoints < 0)
            {
                result.AddError("profile.totalPoints", "Количество очков не может быть отрицательным", "ERR_NEGATIVE_POINTS");
            }
            
            // Проверка времени создания
            if (profile.CreatedAt <= 0)
            {
                result.AddError("profile.createdAt", "Время создания профиля должно быть положительным числом", "ERR_INVALID_CREATED_AT");
            }
            
            // Проверка времени последней активности
            if (profile.LastActive <= 0)
            {
                result.AddError("profile.lastActive", "Время последней активности должно быть положительным числом", "ERR_INVALID_LAST_ACTIVE");
            }
            else if (profile.LastActive < profile.CreatedAt)
            {
                result.AddError("profile.lastActive", "Время последней активности не может быть раньше времени создания профиля", "ERR_LAST_ACTIVE_BEFORE_CREATED");
            }
            
            // Проверка настроек
            if (profile.Settings == null)
            {
                result.AddError("profile.settings", "Настройки пользователя не могут быть пустыми", "ERR_NULL_SETTINGS");
            }
        }
        
        /// <summary>
        /// Выполняет валидацию текущей эмоции пользователя
        /// </summary>
        /// <param name="emotion">Текущая эмоция для валидации</param>
        /// <param name="result">Результат валидации для добавления ошибок</param>
        private void ValidateCurrentEmotion(CurrentEmotion emotion, ValidationResult result)
        {
            // Проверка типа эмоции
            if (string.IsNullOrEmpty(emotion.Type))
            {
                result.AddError("currentEmotion.type", "Тип текущей эмоции не может быть пустым", "ERR_EMPTY_CURRENT_EMOTION_TYPE");
            }
            
            // Проверка интенсивности
            if (emotion.Intensity < 0 || emotion.Intensity > 100)
            {
                result.AddError("currentEmotion.intensity", "Интенсивность текущей эмоции должна быть в диапазоне от 0 до 100", "ERR_INVALID_CURRENT_EMOTION_INTENSITY");
            }
            
            // Проверка временной метки
            if (emotion.Timestamp <= 0)
            {
                result.AddError("currentEmotion.timestamp", "Временная метка текущей эмоции должна быть положительным числом", "ERR_INVALID_CURRENT_EMOTION_TIMESTAMP");
            }
            else if (emotion.Timestamp > DateTime.UtcNow.ToFileTimeUtc())
            {
                result.AddError("currentEmotion.timestamp", "Временная метка текущей эмоции не может быть в будущем", "ERR_FUTURE_CURRENT_EMOTION_TIMESTAMP");
            }
        }
        
        /// <summary>
        /// Проверяет, является ли строка действительным email-адресом
        /// </summary>
        /// <param name="email">Строка для проверки</param>
        /// <returns>true если строка представляет допустимый email, иначе false</returns>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
                
            return EmailRegex.IsMatch(email);
        }
    }
} 