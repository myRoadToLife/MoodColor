namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Источники получения очков
    /// </summary>
    public enum PointsSource
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
        /// Повышение уровня
        /// </summary>
        LevelUp,
        
        /// <summary>
        /// Специальное событие
        /// </summary>
        SpecialEvent,
        
        /// <summary>
        /// Приглашение друга
        /// </summary>
        FriendInvite,
        
        /// <summary>
        /// Трата очков
        /// </summary>
        Spending,

        /// <summary>
        /// Взаимодействие с банкой (клик)
        /// </summary>
        JarInteraction
    }
} 