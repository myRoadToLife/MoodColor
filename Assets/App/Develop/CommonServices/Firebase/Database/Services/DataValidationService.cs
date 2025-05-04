using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Services;
using UnityEngine;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис валидации данных для Firebase
    /// </summary>
    public class DataValidationService
    {
        private readonly Dictionary<Type, object> _validators = new Dictionary<Type, object>();
        
        /// <summary>
        /// Регистрирует валидатор данных определенного типа
        /// </summary>
        /// <param name="validator">Валидатор данных</param>
        /// <typeparam name="T">Тип данных для валидации</typeparam>
        public void RegisterValidator<T>(IDataValidator<T> validator)
        {
            var type = typeof(T);
            if (_validators.ContainsKey(type))
            {
                Debug.LogWarning($"Валидатор для типа {type.Name} уже зарегистрирован. Будет использован новый валидатор.");
            }
            
            _validators[type] = validator;
            Debug.Log($"Зарегистрирован валидатор для типа {type.Name}");
        }
        
        /// <summary>
        /// Проверяет, доступен ли валидатор для указанного типа данных
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <returns>true если валидатор зарегистрирован, иначе false</returns>
        public bool HasValidator<T>()
        {
            return _validators.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Выполняет валидацию данных
        /// </summary>
        /// <param name="data">Данные для валидации</param>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <returns>Результат валидации</returns>
        /// <exception cref="InvalidOperationException">Если валидатор не зарегистрирован</exception>
        public ValidationResult Validate<T>(T data)
        {
            var type = typeof(T);
            if (!_validators.TryGetValue(type, out var validatorObj))
            {
                throw new InvalidOperationException($"Валидатор для типа {type.Name} не зарегистрирован");
            }
            
            var validator = (IDataValidator<T>)validatorObj;
            var result = validator.Validate(data);
            
            if (!result.IsValid)
            {
                Debug.LogWarning($"Валидация данных типа {type.Name} не пройдена. Ошибок: {result.Errors.Count}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверяет валидность данных
        /// </summary>
        /// <param name="data">Данные для проверки</param>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <returns>true если данные валидны, иначе false</returns>
        /// <exception cref="InvalidOperationException">Если валидатор не зарегистрирован</exception>
        public bool IsValid<T>(T data)
        {
            var type = typeof(T);
            if (!_validators.TryGetValue(type, out var validatorObj))
            {
                throw new InvalidOperationException($"Валидатор для типа {type.Name} не зарегистрирован");
            }
            
            var validator = (IDataValidator<T>)validatorObj;
            return validator.IsValid(data);
        }
        
        /// <summary>
        /// Пытается выполнить валидацию данных без выбрасывания исключения
        /// </summary>
        /// <param name="data">Данные для валидации</param>
        /// <param name="result">Результат валидации (если успешно)</param>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <returns>true если валидатор зарегистрирован и валидация выполнена, иначе false</returns>
        public bool TryValidate<T>(T data, out ValidationResult result)
        {
            var type = typeof(T);
            if (!_validators.TryGetValue(type, out var validatorObj))
            {
                result = null;
                return false;
            }
            
            var validator = (IDataValidator<T>)validatorObj;
            result = validator.Validate(data);
            return true;
        }
    }
} 