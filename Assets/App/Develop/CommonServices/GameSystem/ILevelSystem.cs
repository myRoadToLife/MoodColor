using System;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Интерфейс системы уровней
    /// </summary>
    public interface ILevelSystem
    {
        /// <summary>
        /// Текущий уровень пользователя
        /// </summary>
        int CurrentLevel { get; }
        
        /// <summary>
        /// Текущее количество опыта
        /// </summary>
        int CurrentXP { get; }
        
        /// <summary>
        /// Требуемое количество опыта для следующего уровня
        /// </summary>
        int RequiredXPForNextLevel { get; }
        
        /// <summary>
        /// Прогресс к следующему уровню (0.0 - 1.0)
        /// </summary>
        float LevelProgress { get; }
        
        /// <summary>
        /// Событие повышения уровня
        /// </summary>
        event Action<int> OnLevelUp;
        
        /// <summary>
        /// Событие изменения опыта (текущий опыт, полученный опыт)
        /// </summary>
        event Action<int, int> OnXPChanged;
        
        /// <summary>
        /// Добавить опыт
        /// </summary>
        /// <param name="amount">Количество опыта</param>
        /// <param name="source">Источник опыта</param>
        void AddXP(int amount, XPSource source);
        
        /// <summary>
        /// Рассчитать необходимое количество опыта для указанного уровня
        /// </summary>
        /// <param name="level">Уровень</param>
        /// <returns>Количество опыта</returns>
        int CalculateRequiredXP(int level);
        
        /// <summary>
        /// Получить множитель опыта для указанного источника
        /// </summary>
        /// <param name="source">Источник опыта</param>
        /// <returns>Множитель</returns>
        float GetXPMultiplier(XPSource source);
    }
} 