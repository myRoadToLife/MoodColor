using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с баночками эмоций в Firebase Database
    /// </summary>
    public interface IJarDatabaseService
    {
        /// <summary>
        /// Получает все баночки пользователя
        /// </summary>
        Task<Dictionary<string, JarData>> GetUserJars();
        
        /// <summary>
        /// Обновляет баночку эмоций
        /// </summary>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="jar">Данные баночки</param>
        Task UpdateJar(string emotionType, JarData jar);
        
        /// <summary>
        /// Обновляет количество эмоций в баночке
        /// </summary>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="amountToAdd">Количество для добавления (может быть отрицательным)</param>
        Task UpdateJarAmount(string emotionType, int amountToAdd);
        
        /// <summary>
        /// Обновляет уровень баночки
        /// </summary>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="level">Новый уровень</param>
        Task UpdateJarLevel(string emotionType, int level);
        
        /// <summary>
        /// Обновляет кастомизацию баночки
        /// </summary>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="customization">Данные кастомизации</param>
        Task UpdateJarCustomization(string emotionType, JarCustomization customization);
    }
} 