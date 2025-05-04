namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Источники получения опыта
    /// </summary>
    public enum XPSource
    {
        /// <summary>
        /// Отметка эмоции
        /// </summary>
        EmotionMarked,
        
        /// <summary>
        /// Ежедневный бонус
        /// </summary>
        DailyBonus,
        
        /// <summary>
        /// Достижение
        /// </summary>
        Achievement,
        
        /// <summary>
        /// Последовательное использование
        /// </summary>
        ConsecutiveUse,
        
        /// <summary>
        /// Специальное действие (заполнение профиля и т.д.)
        /// </summary>
        SpecialAction
    }
} 