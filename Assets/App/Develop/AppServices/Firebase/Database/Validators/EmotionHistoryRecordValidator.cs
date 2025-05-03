using System;
using App.Develop.AppServices.Firebase.Database.Interfaces;
using App.Develop.AppServices.Firebase.Database.Models;

namespace App.Develop.AppServices.Firebase.Database.Validators
{
    /// <summary>
    /// Валидатор для записей истории эмоций
    /// </summary>
    public class EmotionHistoryRecordValidator : IDataValidator<EmotionHistoryRecord>
    {
        private const long MIN_TIMESTAMP = 132000000000000000; // Примерно 2019 год
        private const float MIN_INTENSITY = 0f;
        private const float MAX_INTENSITY = 1f;
        private const float MIN_VALUE = -1f;
        private const float MAX_VALUE = 1f;
        
        /// <summary>
        /// Выполняет комплексную валидацию записи истории эмоций
        /// </summary>
        /// <param name="record">Запись для валидации</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(EmotionHistoryRecord record)
        {
            var result = new ValidationResult();
            
            if (record == null)
            {
                result.AddError("record", "Запись истории эмоций не может быть пустой", "ERR_NULL_RECORD");
                return result;
            }
            
            // Проверка ID
            if (string.IsNullOrEmpty(record.Id))
            {
                result.AddError("id", "ID записи не может быть пустым", "ERR_EMPTY_ID");
            }
            else if (record.Id.Length < 8 || record.Id.Length > 64)
            {
                result.AddError("id", "ID записи должен быть от 8 до 64 символов", "ERR_INVALID_ID_LENGTH");
            }
            
            // Проверка типа эмоции
            if (string.IsNullOrEmpty(record.Type))
            {
                result.AddError("type", "Тип эмоции не может быть пустым", "ERR_EMPTY_TYPE");
            }
            
            // Проверка интенсивности
            if (record.Intensity < MIN_INTENSITY || record.Intensity > MAX_INTENSITY)
            {
                result.AddError("intensity", 
                    $"Интенсивность эмоции должна быть в диапазоне от {MIN_INTENSITY} до {MAX_INTENSITY}", 
                    "ERR_INVALID_INTENSITY");
            }
            
            // Проверка значения
            if (record.Value < MIN_VALUE || record.Value > MAX_VALUE)
            {
                result.AddError("value", 
                    $"Значение эмоции должно быть в диапазоне от {MIN_VALUE} до {MAX_VALUE}", 
                    "ERR_INVALID_VALUE");
            }
            
            // Проверка цвета
            if (string.IsNullOrEmpty(record.ColorHex))
            {
                result.AddError("colorHex", "Цвет эмоции не может быть пустым", "ERR_EMPTY_COLOR");
            }
            else if (!IsValidHexColor(record.ColorHex))
            {
                result.AddError("colorHex", "Недопустимый формат цвета", "ERR_INVALID_COLOR_FORMAT");
            }
            
            // Проверка временной метки
            if (record.Timestamp <= 0)
            {
                result.AddError("timestamp", "Временная метка должна быть положительным числом", "ERR_INVALID_TIMESTAMP");
            }
            else if (record.Timestamp < MIN_TIMESTAMP)
            {
                result.AddError("timestamp", "Временная метка слишком старая", "ERR_OLD_TIMESTAMP");
            }
            else if (record.Timestamp > DateTime.UtcNow.ToFileTimeUtc())
            {
                result.AddError("timestamp", "Временная метка не может быть в будущем", "ERR_FUTURE_TIMESTAMP");
            }
            
            // Проверка заметки (если есть)
            if (record.Note != null && record.Note.Length > 500)
            {
                result.AddError("note", "Заметка не может превышать 500 символов", "ERR_NOTE_TOO_LONG");
            }
            
            // Проверка локации (если указана)
            if (record.Latitude.HasValue && (record.Latitude < -90 || record.Latitude > 90))
            {
                result.AddError("latitude", "Широта должна быть в диапазоне от -90 до 90", "ERR_INVALID_LATITUDE");
            }
            
            if (record.Longitude.HasValue && (record.Longitude < -180 || record.Longitude > 180))
            {
                result.AddError("longitude", "Долгота должна быть в диапазоне от -180 до 180", "ERR_INVALID_LONGITUDE");
            }
            
            // Проверка статуса синхронизации
            if (!Enum.IsDefined(typeof(SyncStatus), record.SyncStatus))
            {
                result.AddError("syncStatus", "Недопустимый статус синхронизации", "ERR_INVALID_SYNC_STATUS");
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверяет, является ли запись истории эмоций валидной
        /// </summary>
        /// <param name="record">Запись для проверки</param>
        /// <returns>true если запись валидна, иначе false</returns>
        public bool IsValid(EmotionHistoryRecord record)
        {
            return Validate(record).IsValid;
        }
        
        /// <summary>
        /// Проверяет, является ли строка действительным цветом в формате HEX
        /// </summary>
        /// <param name="color">Строка для проверки</param>
        /// <returns>true если строка представляет допустимый HEX-цвет, иначе false</returns>
        private bool IsValidHexColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return false;

            if (color[0] == '#')
                color = color.Substring(1);

            if (color.Length != 6 && color.Length != 8)
                return false;

            foreach (char c in color)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }

            return true;
        }
    }
} 