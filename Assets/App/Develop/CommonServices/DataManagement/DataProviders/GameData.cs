using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using App.Develop.CommonServices.GameSystem;
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    /// <summary>
    /// Данные игровой системы
    /// </summary>
    [Serializable]
    public class GameData : ISaveData
    {
        /// <summary>
        /// Текущее количество очков
        /// </summary>
        [JsonProperty("points")]
        public int Points { get; set; }
        
        /// <summary>
        /// Общее количество заработанных очков за всё время
        /// </summary>
        public int TotalEarnedPoints;
        
        /// <summary>
        /// История транзакций с очками
        /// </summary>
        public List<PointsTransaction> PointsTransactions;
        
        /// <summary>
        /// Последняя дата получения ежедневного бонуса
        /// </summary>
        public DateTime LastDailyBonusDate;
        
        /// <summary>
        /// Список достижений пользователя
        /// </summary>
        public List<Achievement> Achievements;
        
        /// <summary>
        /// Словарь для быстрого доступа к достижениям по ID
        /// </summary>
        [NonSerialized]
        public Dictionary<string, Achievement> AchievementsMap;
        
        /// <summary>
        /// Текущий уровень пользователя
        /// </summary>
        public int Level;
        
        /// <summary>
        /// Текущее количество опыта
        /// </summary>
        public int XP;
        
        /// <summary>
        /// Последнее обновление опыта
        /// </summary>
        public DateTime LastXpUpdateDate;
        
        [JsonProperty("lastUpdated")]
        public long LastUpdatedTimestamp { get; set; }
        
        [JsonIgnore]
        public DateTime LastUpdated 
        {
            get => LastUpdatedTimestamp > 0 ? DateTime.FromFileTimeUtc(LastUpdatedTimestamp) : DateTime.MinValue;
            set => LastUpdatedTimestamp = value.ToFileTimeUtc();
        }
        
        public GameData()
        {
            Points = 0;
            TotalEarnedPoints = 0;
            PointsTransactions = new List<PointsTransaction>();
            LastDailyBonusDate = DateTime.MinValue;
            Achievements = new List<Achievement>();
            AchievementsMap = new Dictionary<string, Achievement>();
            Level = 1; // Стартовый уровень - 1
            XP = 0;
            LastXpUpdateDate = DateTime.MinValue;
            LastUpdated = DateTime.UtcNow;
        }

        // Метод для клонирования объекта
        public GameData Clone()
        {
            return new GameData
            {
                Points = this.Points,
                TotalEarnedPoints = this.TotalEarnedPoints,
                PointsTransactions = new List<PointsTransaction>(this.PointsTransactions),
                LastDailyBonusDate = this.LastDailyBonusDate,
                Achievements = new List<Achievement>(this.Achievements),
                AchievementsMap = new Dictionary<string, Achievement>(this.AchievementsMap),
                Level = this.Level,
                XP = this.XP,
                LastXpUpdateDate = this.LastXpUpdateDate,
                LastUpdatedTimestamp = this.LastUpdatedTimestamp
            };
        }
    }
} 