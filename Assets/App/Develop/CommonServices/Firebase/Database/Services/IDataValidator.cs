using System.Collections.Generic;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Базовый интерфейс для валидаторов данных
    /// </summary>
    /// <typeparam name="T">Тип данных для валидации</typeparam>
    public interface IDataValidator<T>
    {
        /// <summary>
        /// Выполняет валидацию данных
        /// </summary>
        /// <param name="data">Данные для валидации</param>
        /// <returns>Результат валидации с возможными ошибками</returns>
        ValidationResult Validate(T data);
        
        /// <summary>
        /// Проверяет, являются ли данные валидными
        /// </summary>
        /// <param name="data">Данные для проверки</param>
        /// <returns>true если данные валидны, иначе false</returns>
        bool IsValid(T data);
    }
    
    /// <summary>
    /// Результат валидации данных
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Флаг валидности данных
        /// </summary>
        public bool IsValid => Errors.Count == 0;
        
        /// <summary>
        /// Список ошибок валидации
        /// </summary>
        public List<ValidationError> Errors { get; } = new List<ValidationError>();
        
        /// <summary>
        /// Добавляет ошибку валидации
        /// </summary>
        /// <param name="fieldName">Имя поля с ошибкой</param>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="errorCode">Код ошибки</param>
        public void AddError(string fieldName, string message, string errorCode = null)
        {
            Errors.Add(new ValidationError(fieldName, message, errorCode));
        }
    }
    
    /// <summary>
    /// Ошибка валидации данных
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Имя поля с ошибкой
        /// </summary>
        public string FieldName { get; }
        
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Код ошибки (опционально)
        /// </summary>
        public string ErrorCode { get; }
        
        public ValidationError(string fieldName, string message, string errorCode = null)
        {
            FieldName = fieldName;
            Message = message;
            ErrorCode = errorCode;
        }
    }
} 