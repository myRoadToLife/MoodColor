using System;
using UnityEngine;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Класс, представляющий достижение
    /// </summary>
    [Serializable]
    public class Achievement
    {
        /// <summary>
        /// Уникальный идентификатор достижения
        /// </summary>
        public string Id;
        
        /// <summary>
        /// Название достижения
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Описание достижения
        /// </summary>
        public string Description;
        
        /// <summary>
        /// Тип достижения
        /// </summary>
        public AchievementType Type;
        
        /// <summary>
        /// Иконка достижения
        /// </summary>
        [NonSerialized]
        public Sprite Icon;
        
        /// <summary>
        /// Путь к иконке достижения
        /// </summary>
        public string IconPath;
        
        /// <summary>
        /// Награда в очках за выполнение достижения
        /// </summary>
        public int PointsReward;
        
        /// <summary>
        /// Прогресс выполнения (от 0.0f до 1.0f)
        /// </summary>
        public float Progress;
        
        /// <summary>
        /// Завершено ли достижение
        /// </summary>
        public bool IsCompleted;
        
        /// <summary>
        /// Дата завершения достижения
        /// </summary>
        public DateTime? CompletionDate;
    }
} 