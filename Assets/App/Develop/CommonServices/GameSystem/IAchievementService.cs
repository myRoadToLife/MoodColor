using System;
using System.Collections.Generic;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Интерфейс сервиса управления достижениями
    /// </summary>
    public interface IAchievementService
    {
        /// <summary>
        /// Событие при выполнении достижения
        /// </summary>
        event Action<Achievement> OnAchievementCompleted;
        
        /// <summary>
        /// Событие при обновлении прогресса достижения
        /// </summary>
        event Action<Achievement, float> OnAchievementProgressUpdated;
        
        /// <summary>
        /// Получить все достижения
        /// </summary>
        /// <returns>Список всех достижений</returns>
        List<Achievement> GetAllAchievements();
        
        /// <summary>
        /// Получить завершенные достижения
        /// </summary>
        /// <returns>Список завершенных достижений</returns>
        List<Achievement> GetCompletedAchievements();
        
        /// <summary>
        /// Получить незавершенные достижения
        /// </summary>
        /// <returns>Список незавершенных достижений</returns>
        List<Achievement> GetInProgressAchievements();
        
        /// <summary>
        /// Получить прогресс выполнения достижения
        /// </summary>
        /// <param name="achievementId">Идентификатор достижения</param>
        /// <returns>Прогресс от 0.0f до 1.0f</returns>
        float GetAchievementProgress(string achievementId);
        
        /// <summary>
        /// Обновить прогресс достижения
        /// </summary>
        /// <param name="achievementId">Идентификатор достижения</param>
        /// <param name="progress">Новый прогресс</param>
        void UpdateAchievementProgress(string achievementId, float progress);
        
        /// <summary>
        /// Проверить выполнение всех достижений
        /// </summary>
        void CheckAllAchievements();
        
        /// <summary>
        /// Получить достижение по идентификатору
        /// </summary>
        /// <param name="achievementId">Идентификатор достижения</param>
        /// <returns>Достижение или null, если не найдено</returns>
        Achievement GetAchievement(string achievementId);
    }
} 